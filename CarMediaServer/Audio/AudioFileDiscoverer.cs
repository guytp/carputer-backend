using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

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
		#endregion

		#region Properties
		/// <summary>
		/// Gets a list of UUIDs for any devices that are currently mounted and available.
		/// </summary>
		public string[] MountedDeviceUuids
		{
			get
			{
				List<string> allDevices = new List<string>();
				lock (_discoveryThreads)
				{
					foreach (MountedDevice device in _discoveryThreads.Keys)
						allDevices.Add(device.Uuid);
				}
				return allDevices.ToArray();
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
		private void MountManagerOnUnmounted (object sender, MountedDeviceEventArgs e)
		{
			Logger.Debug("Unmounted " + e.Device.Device + "   from   " + e.Device.MountPath);
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
			// Cast object from thread arguments
			MountedDevice device = (MountedDevice)args;

			// Get a list from database of all existing tracks
			AudioFile[] allAudioFiles = AudioFileFactory.ApplicationInstance.ReadAll ().Where (file => file.DeviceUuid == device.Uuid).ToArray();

			// Itterate through each file on disk recursively.  If a new track is found add its data to the list,
			// if an existing track is found mark it as still present
			DateTime startTime = DateTime.UtcNow;
			RecursiveDiscover (device, "/", allAudioFiles);
			Console.WriteLine ("Discovered everything on " + device.MountPath);

			// Finally remove any tracks that are no longer on the media
			AudioFileFactory.ApplicationInstance.RemoveForUuid (device.Uuid, startTime);
		}
		#endregion

		#region Helper Methods
		/// <summary>
		/// Recursively probe all MP3s within a directory.  Each file that is found either updates the existing list or
		/// creates a new entry in the database.
		/// </summary>
		/// <param name='device'>
		/// The device that these MP3s are contained within.
		/// </param>
		/// <param name='subPath'>
		/// The sub-path that this recursion is relative to the root of the device's mount path.
		/// </param>
		/// <param name='allAudioFiles'>
		/// All current audio tracks stored in the database.
		/// </param>
		/// <returns>
		/// A list of MP3s found in this folder and all sub-folders.
		/// </returns>
		private List<AudioFile> RecursiveDiscover (MountedDevice device, string subPath, AudioFile[] allAudioFiles)
		{
			// List to store all discoveries
			List<AudioFile> files = new List<AudioFile> ();

			// Probe all subdirectories
			string path = device.MountPath + subPath;
			string[] directories = Directory.GetDirectories (path);
			foreach (string directory in directories)
				files.AddRange (RecursiveDiscover (device, subPath + (subPath.EndsWith ("/") ? null : "/") + Path.GetFileName (directory), allAudioFiles));

			// Now probe each file within this directory
			string[] mp3files = Directory.GetFiles (path, "*.mp3");
			foreach (string file in mp3files) {
				try
				{
					// Make an external call to mp3 info to parse the information
					ProcessStartInfo startInfo = new ProcessStartInfo("mp3info", "-p \"%a\n%l\n%n\n%t\n%S\" \"" + file + "\"");
					startInfo.RedirectStandardOutput = true;
					startInfo.RedirectStandardError = true;
					startInfo.UseShellExecute = false;
					Process process = Process.Start (startInfo);
					process.WaitForExit ();
					if (process.ExitCode != 0)
						break;

					// Read the data we got back from mp3info to create a managed object
					string processOutput = process.StandardOutput.ReadToEnd ();
					string[] processOutputLines = processOutput.Split (new char[] { '\n' });
					if (processOutputLines.Length != 5)
						break;
					AudioFile audioFile = new AudioFile();
					audioFile.Album = processOutputLines[1];
					audioFile.Artist = string.IsNullOrEmpty(processOutputLines[0]) ? Path.GetFileName(Path.GetDirectoryName(file)) : processOutputLines[0];
					audioFile.DeviceUuid = device.Uuid;
					int durationSeconds;
					if (int.TryParse (processOutputLines[4], out durationSeconds))
						audioFile.Duration = TimeSpan.FromSeconds (durationSeconds);
					audioFile.LastSeen = DateTime.UtcNow;
					audioFile.RelativePath = Path.Combine (subPath, Path.GetFileName (file));
					audioFile.Title = string.IsNullOrEmpty(processOutputLines[3]) ? Path.GetFileNameWithoutExtension(file) : processOutputLines[3];
					int trackNumber;
					if (int.TryParse (processOutputLines[2], out trackNumber))
						audioFile.TrackNumber = trackNumber;
					files.Add (audioFile);
					Logger.Debug(audioFile.ToString ());

					// See if we have an existing file for this one
					AudioFile existing = allAudioFiles.Where (existingFile => existingFile.RelativePath == audioFile.RelativePath).FirstOrDefault();
					if (existing != null)
						audioFile.AudioFileId = existing.AudioFileId;

					// Update (or create) in the database
					if (audioFile.AudioFileId == 0)
						AudioFileFactory.ApplicationInstance.Create (audioFile);
					else
						AudioFileFactory.ApplicationInstance.Update (audioFile);
				}
				catch (Exception ex)
				{
					Logger.Error ("Failed to probe file " + file, ex);
				}
			}

			// Return everything we have discovered and all recursive calls
			return files;
		}
		#endregion
	}
}