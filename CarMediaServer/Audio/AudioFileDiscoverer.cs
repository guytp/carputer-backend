using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CarMediaServer
{
	/// <summary>
	/// This class is used to probe audio files on devices as they become attached to the system.  This is then
	/// merged with the audio database to ensure we have the latest details available whenever a device is added.
	/// When a device is removed any scanning on it will stop.
	/// </summary>
	public class AudioFileDiscoverer
	{
		#region Declarations
		/// <summary>
		/// Defines the mount manager to use for device discovery.
		/// </summary>
		private MountManager _mountManager;

		/// <summary>
		/// Defines a map between mounted devices and their currently running discovery threads.
		/// </summary>
		private readonly Dictionary<MountedDevice, Thread> _discoveryThreads = new Dictionary<MountedDevice, Thread>();

		/// <summary>
		/// Defines a list of the UUIDs of mounted devices that we have started or completed discovery on.
		/// </summary>
		private readonly List<string> _mountedDeviceUuids = new List<string>();
		#endregion

		#region Properties
		/// <summary>
		/// Gets a list of UUIDs for any devices that are currently mounted and available.
		/// </summary>
		public string[] MountedDeviceUuids
		{
			get
			{
				lock (_mountedDeviceUuids)
				{
					return _mountedDeviceUuids.ToArray();
				}
			}
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new instance of this class.
		/// </summary>
		public AudioFileDiscoverer()
		{
			// Create mount manager
			if (Controller.MountManager == null)
				Controller.MountManager = new MountManager();
			_mountManager = Controller.MountManager;

			// Hook-up to events
			_mountManager.DeviceMounted += MountManagerOnMounted;
			_mountManager.DeviceUnmounted += MountManagerOnUnmounted;

			// Start the mount manager if needed
			if ((!_mountManager.IsStarted) && (!_mountManager.IsStarting))
				_mountManager.Start();
		}
		#endregion

		#region Event Handlers
		/// <summary>
		/// Handle a device being mounted.
		/// </summary>
		/// <param name='sender'>
		/// The event sender.
		/// </param>
		/// <param name='e'>
		/// The event arguments.
		/// </param>
		private void MountManagerOnMounted (object sender, MountedDeviceEventArgs e)
		{
			Logger.Debug("Mounted " + e.Device.Device + "   to   " + e.Device.MountPath);
			Thread discoveryThread = new Thread(AudioDiscoveryThread) { IsBackground = true, Name = "Audio Discovery " + e.Device.Device };
			lock (_discoveryThreads)
				_discoveryThreads.Add (e.Device, discoveryThread);
			lock (_mountedDeviceUuids)
				_mountedDeviceUuids.Add(e.Device.Uuid);
			Controller.AudioPlayer.NotifyDeviceOffline(e.Device.Uuid);
			discoveryThread.Start (e.Device);
		}
		
		/// <summary>
		/// Handle a device being unmounted.
		/// </summary>
		/// <param name='sender'>
		/// The event sender.
		/// </param>
		/// <param name='e'>
		/// The event arguments.
		/// </param>
		private void MountManagerOnUnmounted(object sender, MountedDeviceEventArgs e)
		{
			Logger.Debug("Unmounted " + e.Device.Device + "   from   " + e.Device.MountPath);
			lock (_mountedDeviceUuids)
				if (_mountedDeviceUuids.Contains(e.Device.Uuid))
					_mountedDeviceUuids.Remove(e.Device.Uuid);
			lock (_discoveryThreads)
			{
				if (_discoveryThreads.ContainsKey(e.Device))
				{
					// Force the thread to terminate
					try
					{
						_discoveryThreads[e.Device].Abort();
					}
					catch
					{
					}
					_discoveryThreads.Remove(e.Device);
				}
			}

			// Get all files for this audio source and set them offline
			AudioFile[] filesOnDevice = AudioFileFactory.ApplicationInstance.ReadAll().Where(audioFile => audioFile.DeviceUuid == e.Device.Uuid).ToArray();
			if (filesOnDevice.Length < 1)
				return;
			AudioLibraryUpdateNotification notification = new AudioLibraryUpdateNotification();
			foreach (AudioFile audioFile in filesOnDevice)
				notification.OfflineFiles.Add(audioFile.AudioFileId);
			Controller.NotificationNetworkServer.SendNotification(notification);
		}
		#endregion

		#region Threads
		/// <summary>
		/// This thread is used to probe all files on a particular device.
		/// </summary>
		/// <param name='args'>
		/// The arguments to pass to the thread.
		/// </param>
		private void AudioDiscoveryThread(object args)
		{
			try
			{
				// Cast object from thread arguments
				MountedDevice device = (MountedDevice)args;

				// Get a list from database of all existing tracks
				AudioFile[] allAudioFiles = AudioFileFactory.ApplicationInstance.ReadAll().Where(file => file.DeviceUuid == device.Uuid).ToArray();

				// Recursively gain two lists - new and existing files.  We always process new files
				// first, then online, then we check for changed to online and fire updated
				// and finally deleted to give quickest response to the client
				List<string> newFiles = new List<string>();
				List<string> existingFiles = new List<string>();
				GetAllFiles(device, "/", allAudioFiles, newFiles, existingFiles);

				// Now process the file sets
				DateTime startTime = DateTime.UtcNow;
				Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
				ProcessFileList(device, newFiles, allAudioFiles);
				AudioLibraryUpdateNotification notification = new AudioLibraryUpdateNotification();
				if (existingFiles.Count > 0)
				{
					foreach (string relativePath in existingFiles)
					{
						AudioFile file = allAudioFiles.FirstOrDefault(f => f.RelativePath == relativePath);
						if (file == null)
							continue;
						notification.OnlineFiles.Add(file);
					}
					ProcessFileChanges(notification);
					notification = new AudioLibraryUpdateNotification();
				}
				Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
				ProcessFileList(device, existingFiles, allAudioFiles);
				Thread.CurrentThread.Priority = ThreadPriority.Normal;
				foreach (AudioFile file in allAudioFiles)
					if (file.LastSeen < startTime)
						notification.DeletedFiles.Add(file.AudioFileId);
				if (notification.DeletedFiles.Count > 0)
					Controller.NotificationNetworkServer.SendNotification(notification);
				AudioFileFactory.ApplicationInstance.RemoveForUuid(device.Uuid, startTime);
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception ex)
			{
				Logger.Error("Fatal error during audio file discovery", ex);
			}
			finally
			{
				lock (_discoveryThreads)
				{
					MountedDevice key = null;
					foreach (KeyValuePair<MountedDevice, Thread> kvp in _discoveryThreads)
					{
						if (kvp.Value == Thread.CurrentThread)
						{
							key = kvp.Key;
							break;
						}
					}
					if ((key != null) && (_discoveryThreads.ContainsKey(key)))
						_discoveryThreads.Remove(key);
				}
			}
		}
		#endregion

		#region Helper Methods
		private void GetAllFiles(MountedDevice device, string subPath, AudioFile[] allAudioFiles, List<string> newFiles, List<string> existingFiles)
		{
			// Probe all subdirectories
			string path = device.MountPath + subPath;
			string[] directories = null;
			try
			{
				directories = Directory.GetDirectories(path);
				foreach (string directory in directories)
					try
					{
						GetAllFiles(device, subPath + (subPath.EndsWith("/") ? null : "/") + Path.GetFileName(directory), allAudioFiles, newFiles, existingFiles);
					}
					catch
					{
					}

				
				string[] mp3files = Directory.GetFiles(path, "*.mp3");
				if (mp3files.Length < 1)
					return;
				foreach (string file in mp3files)
				{
					string relativePath = Path.Combine(subPath, Path.GetFileName(file));
					if (allAudioFiles.Any(audioFile => audioFile.RelativePath == relativePath))
						existingFiles.Add(relativePath);
					else
						newFiles.Add(relativePath);
				}
			}
			catch
			{
			}
		}

		private void ProcessFileList(MountedDevice device, List<string> mp3Files, AudioFile[] allAudioFiles)
		{
			// Create a new notification object if required
			AudioLibraryUpdateNotification notification = new AudioLibraryUpdateNotification();

			// Now probe each file within this directory
			try
			{
				foreach (string relativePath in mp3Files)
				{
					string file = Path.Combine(device.MountPath + relativePath);
					if (!File.Exists(file))
					{
						Logger.Error("File no longer exists when probing mounted file " + file + " for audio information, aborting loop");
						break;
					}
					try
					{
						AudioFile audioFile = new AudioFile();
						using (TagLib.File tagFile = TagLib.File.Create(file))
						{
							if (!string.IsNullOrEmpty(tagFile.Tag.Album))
								audioFile.Album = tagFile.Tag.Album;
							if (!string.IsNullOrEmpty(tagFile.Tag.JoinedAlbumArtists))
								audioFile.Artist = tagFile.Tag.JoinedAlbumArtists;
							if (tagFile.Properties.Duration != TimeSpan.Zero)
								audioFile.Duration = tagFile.Properties.Duration;
							if (!string.IsNullOrEmpty(tagFile.Tag.Title))
								audioFile.Title = tagFile.Tag.Title;
							if (tagFile.Tag.Track != 0)
								audioFile.TrackNumber = (int)tagFile.Tag.Track;
						}
						if (string.IsNullOrEmpty(audioFile.Artist))
						{
							string[] parts = relativePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
							int depth = parts.Length - 1;
							string partToParse = parts[depth < 2 ? 0 : depth - 2];
							if (partToParse.Contains("-"))
								partToParse = partToParse.Split(new char[] { '-' })[0].Trim();
							audioFile.Artist = partToParse;
							if (audioFile.Artist == string.Empty)
								audioFile.Artist = null;
						}
						if (string.IsNullOrEmpty(audioFile.Album))
						{
							string[] parts = relativePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
							int depth = parts.Length - 1;
							string partToParse = parts[depth < 1 ? 0 : depth - 1];
							if (partToParse.Contains("-"))
								partToParse = partToParse.Split(new char[] { '-' })[0].Trim();
							audioFile.Album = partToParse;
							if (audioFile.Album == string.Empty)
								audioFile.Album = null;
						}
						if (string.IsNullOrEmpty(audioFile.Title))
						{
							audioFile.Title = Path.GetFileNameWithoutExtension(file).Trim();
							if (audioFile.Title.Contains("-"))
								audioFile.Title = audioFile.Title.Split(new char[] { '-' }, 2)[1].Trim();
							while (!string.IsNullOrEmpty(audioFile.Title) && char.IsDigit(audioFile.Title[0]))
							{
								if (audioFile.Title.Length == 1)
								{
									audioFile.Title = null;
									break;
								}
								audioFile.Title = audioFile.Title.Substring(1, audioFile.Title.Length - 1).Trim();
							}
							if (audioFile.Title == string.Empty)
								audioFile.Title = null;
						}
						audioFile.DeviceUuid = device.Uuid;
						audioFile.LastSeen = DateTime.UtcNow;
						audioFile.RelativePath = relativePath;
						Logger.Debug(audioFile.ToString());

						// See if we have an existing file for this one
						AudioFile existing = allAudioFiles.Where(existingFile => existingFile.RelativePath == audioFile.RelativePath).FirstOrDefault();
						bool existingIsEqual = false;
						if (existing != null)
						{
							existingIsEqual = audioFile.Equals(existing);
							if (!existingIsEqual)
							{
								if ((existing.Album != audioFile.Album) || (existing.Artist != audioFile.Artist))
									existing.ArtworkSearchDate = null;
								existing.Album = audioFile.Album;
								existing.Artist = audioFile.Artist;
								existing.Duration = audioFile.Duration;
								existing.LastSeen = audioFile.LastSeen;
								existing.Title = audioFile.Title;
								existing.TrackNumber = audioFile.TrackNumber;
								audioFile = existing;
							}
						}

						// The "AC/DC Fix"
						if ((audioFile.Artist == "AC;DC") || (audioFile.Artist == "AC; DC") || (audioFile.Artist == "AC_DC"))
							audioFile.Artist = "AC/DC";

						// Add to update notification if new or if it is updated
						if (audioFile.AudioFileId == 0)
							notification.NewFiles.Add(audioFile);
						else if (!existingIsEqual)
							notification.UpdatedFiles.Add(audioFile);

						// Deal database updates and changes and firing of notifications if we have reached enough
						// files in this object and create a new object
						if (notification.ReadyToSend)
						{
							ProcessFileChanges(notification);
							notification = new AudioLibraryUpdateNotification();
						}
					}
					catch (Exception ex)
					{
						Logger.Error("Failed to probe file " + file, ex);
					}
				}
			}
			catch
			{
			}

			// Deal database updates and changes and firing of notifications if we have reached enough
			// files in this object and create a new object
			ProcessFileChanges(notification);
		}

		/// <summary>
		/// Processes a batch of file changes.
		/// </summary>
		/// <param name='notification'>
		/// The notification containing the changes to process.
		/// </param>
		private void ProcessFileChanges(AudioLibraryUpdateNotification notification)
		{
			// If no rows, do nothing
			if ((notification.UpdatedFiles.Count == 0) && (notification.NewFiles.Count == 0) && (notification.OnlineFiles.Count == 0) && (notification.DeletedFiles.Count == 0) && (notification.OfflineFiles.Count == 0))
				return;

			// Merge these changes in the database
			AudioFileFactory.ApplicationInstance.MergeChanges(notification.NewFiles, notification.OnlineFiles, notification.UpdatedFiles);

			// Raise the notification
			Controller.NotificationNetworkServer.SendNotification(notification);
		}
		#endregion
	}
}