using Mono.Unix;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Linq;

namespace CarMediaServer
{
	/// <summary>
	/// This class watches for new devices being added to the system, and then automatically mounts them when they appear
	/// and unmounts them when they are removed.  It ensures all devices are given a constant ID to be tracked between
	/// sessions.
	/// </summary>
	public class MountManager
	{
		#region Declarations
		/// <summary>
		/// Defines the prefix to use for a folder to mount everything in.
		/// </summary>
		private const string _mountPrefix = "/mnt";

		/// <summary>
		/// Defines pre-mounted devices outside our space or devices that failed to mount.
		/// </summary>
		private List<string> _devicesToIgnore = new List<string>();
		
		/// <summary>
		/// Defines mounted devices keyed by the device and storing the UUID.
		/// </summary>
		private Dictionary<string, string> _mountedDevices = new Dictionary<string, string>();

		/// <summary>
		/// Defines the thread that 
		/// </summary>
		private Thread _storageDeviceDetectionThread;
		#endregion

		#region Properties
		/// <summary>
		/// Gets whether or not the mount manager is started.
		/// </summary>
		public bool IsStarted { get; private set; }

		/// <summary>
		/// Gets whether or not the mount manager is starting.
		/// </summary>
		public bool IsStarting { get; private set; }

		/// <summary>
		/// Gets whether or not the mount manager is stopping.
		/// </summary>
		public bool IsStopping { get; private set; }
		#endregion

		#region Events
		/// <summary>
		/// Fired when a device is mounted.
		/// </summary>
		public event EventHandler<MountedDeviceEventArgs> DeviceMounted;

		/// <summary>
		/// Fired when a device is unmounted.
		/// </summary>
		public event EventHandler<MountedDeviceEventArgs> DeviceUnmounted;
		#endregion

		#region Public Control
		/// <summary>
		/// Start the mount manager.
		/// </summary>
		public void Start()
		{
			// Throw error if already started or starting
			if ((IsStarted) || (IsStarting))
				throw new Exception("Mount manager is not stopped");
			IsStarting = true;
			
			// Create mount prefix if it does not exist
			if (!Directory.Exists(_mountPrefix))
				Directory.CreateDirectory(_mountPrefix);

			// Attempt to kill any old mount points and remove anything mounted in are mount path
			Dictionary<string, string> mountedDevices = GetMountedDevices ();
			foreach (KeyValuePair<string, string> mountedDevice in mountedDevices)
			{
				if (mountedDevice.Value.StartsWith (_mountPrefix + "/"))
					Unmount (mountedDevice.Key, mountedDevice.Value);
			}
			string[] oldMountPoints = Directory.GetDirectories (_mountPrefix);
			foreach (string oldMountPoint in oldMountPoints)
			{
				try
				{
					Directory.Delete (oldMountPoint);
				}
				catch
				{
				}
			}

			// Initially determine devices to be ignored
			_devicesToIgnore.AddRange (GetMountedDevices ().Keys);

			// Start a thread that detects devices being added or removed
			_storageDeviceDetectionThread = new Thread(StorageDeviceDetectionThread) { IsBackground = true, Name = "Storage Detection" };
			IsStarted = true;
			_storageDeviceDetectionThread.Start();
			IsStarting = false;
		}

		public void Stop ()
		{
			// Throw error if already stopping or starting
			if (IsStopping)
				throw new Exception ("Mount manager is already stopping");
			if (IsStarting)
				throw new Exception ("Mount manager is currently starting");
			if (!IsStarted)
				throw new Exception ("Mount manager is already stopped");

			// Mark as stopping and give thread 5 seconds to terminate
			IsStopping = true;
			DateTime waitUntil = DateTime.UtcNow.AddSeconds (5);
			while ((DateTime.Now < waitUntil) && (_storageDeviceDetectionThread != null) && (_storageDeviceDetectionThread.ThreadState == System.Threading.ThreadState.Running))
				Thread.Sleep (100);
			try {
				_storageDeviceDetectionThread.Abort ();
			} catch {
			}
			_storageDeviceDetectionThread = null;

			// Mark as stopped
			IsStarted = false;
			IsStopping = false;
		}
		#endregion

		#region Threads
		/// <summary>
		/// This thread is the main thread that checks for devices being added to or removed from the system.
		/// </summary>
		private void StorageDeviceDetectionThread()
		{
			int errorCount = 0;
			DateTime lastErrorTime = new DateTime(0);
			while (IsStarted && !IsStopping)
			{
				try
				{
				// Determine all current available devices by UUID for those that are not in ignore list
				string[] allDevices = Directory.GetFiles ("/dev/disk/by-uuid");
				List<string> allDeviceNames = new List<string>();
				foreach (string uuidPath in allDevices)
				{
					try
					{
						// Get the real device from the symbolic link
						string device = uuidPath;
						Mono.Unix.UnixSymbolicLinkInfo symLink = new Mono.Unix.UnixSymbolicLinkInfo (uuidPath);
						if (symLink.HasContents)
							device = Path.GetFullPath (Path.Combine (Path.GetDirectoryName(device), symLink.ContentsPath));
						allDeviceNames.Add (device);
						
						// If this device is on ignore list, already mounted or already failed skip it
						if ((_devicesToIgnore.Contains (device)) || (_mountedDevices.ContainsKey (device)))
							continue;

						// Mount the device
						string uuid = Path.GetFileName (uuidPath);
						if (!Mount (device, _mountPrefix + "/" + uuid))
							_devicesToIgnore.Add (device);
						else
						{
							_mountedDevices.Add (device, uuid);
							if (DeviceMounted != null)
							{
								DeviceMounted(this, new MountedDeviceEventArgs(new MountedDevice(device, _mountPrefix + "/" + uuid, uuid)));
							}
						}
					}
					catch
					{
					}
				}

				// Determine removed devices and unmount plus update dictionaries
				Dictionary<string, string> allMountedDevices = GetMountedDevices();
				List<string> removedDevices = new List<string>();
				foreach (KeyValuePair<string, string> mountedDevice in _mountedDevices)
				{
					if ((!allMountedDevices.ContainsKey(mountedDevice.Key)) || (!File.Exists (mountedDevice.Key)))
						removedDevices.Add (mountedDevice.Key);
				}
				foreach (string failedDevice in _devicesToIgnore)
				{
					if ((!allDeviceNames.Contains(failedDevice)) || (!File.Exists (failedDevice)))
						removedDevices.Add (failedDevice);
				}
				foreach (string removedDevice in removedDevices)
				{
					// Remove from mappings
					if (_devicesToIgnore.Contains (removedDevice))
						_devicesToIgnore.Remove (removedDevice);
					if (_mountedDevices.ContainsKey (removedDevice))
						_mountedDevices.Remove (removedDevice);

					// Attempt to unmount
					string mountPath = allMountedDevices.ContainsKey (removedDevice) ? allMountedDevices[removedDevice] : null;
					Unmount(removedDevice, mountPath);
					if (DeviceUnmounted != null)
					{
						string uuid = null;
						if ((mountPath != null) && (mountPath.StartsWith (_mountPrefix)))
						    uuid = mountPath.Substring (_mountPrefix.Length + 1);
						DeviceUnmounted(this, new MountedDeviceEventArgs(new MountedDevice(removedDevice, _mountPrefix + uuid, uuid)));
					}
				}

				// Wait for next loop
				Thread.Sleep(500);
				}
				catch
				{
					if (DateTime.UtcNow.Subtract(lastErrorTime).TotalSeconds > 30)
						errorCount = 0;
					errorCount++;
					if (errorCount >= 5)
						throw;
					lastErrorTime = DateTime.UtcNow;
				}
			}

			_storageDeviceDetectionThread = null;
		}
		#endregion

		#region Mount Helpers
		/// <summary>
		/// Gets the mounted path of a specified device.
		/// </summary>
		/// <param name='uuid'>
		/// The device unique identifier.
		/// </param>
		/// <returns>
		/// The mounted path of the device or null if not mounted..
		/// </returns>
		public string GetMountedPath(string uuid)
		{
			Dictionary<string, string> mountedDevices = GetMountedDevices();
			string physicalDevice = null;
			lock (_mountedDevices)
			{
				KeyValuePair<string, string> foundPair = _mountedDevices.FirstOrDefault(kvp => kvp.Value == uuid);
				if (foundPair.Value == uuid)
					physicalDevice = foundPair.Key;
			}
			if (physicalDevice == null)
				return null;
			return mountedDevices.ContainsKey(physicalDevice) ? mountedDevices[physicalDevice] : null;
		}

		/// <summary>
		/// Gets all currently mounted devices on the system.
		/// </summary>
		/// <returns>
		/// A dictionary keying the physical device to its mount path.
		/// </returns>
		private Dictionary<string, string> GetMountedDevices()
		{
			// Read the MTab file initially and load all data
			string[] mtabLines = File.ReadAllLines ("/proc/mounts");
			Dictionary<string, string> returnValue = new Dictionary<string, string>();
			foreach (string line in mtabLines)
			{
				string[] parts = line.Split(new char[] { ' ' });
				string device = string.Empty;
				string mountPath = string.Empty;
				int startIndex = 0;
				bool parsingDevice = true;
				for (int i = 1; i < parts.Length; i++)
				{
					// If previous string ended in / we continue
					if (parts[i - 1].EndsWith ("\\"))
						continue;

					// The previous one didn't end with a / so it's a completed part
					if (parsingDevice)
					{
						// Get the device
						for (int j = startIndex; j < i; j++)
							device += (device == string.Empty ? string.Empty : " ") + parts[j];
						startIndex = i;

						// If the device does not start with a "/" we assume it's not a real path
						if (!device.StartsWith ("/"))
							break;

						// Move on to parsing the mount path
						parsingDevice = false;
					}
					else
					{
						for (int j = startIndex; j < i; j++)
							mountPath += (mountPath == string.Empty ? string.Empty : " ") + parts[j];
						break;
					}
				}

				// Continue if we don't have a mount path and device
				if ((string.IsNullOrEmpty (device)) || (string.IsNullOrEmpty (mountPath)))
					continue;

				// If the device name is a symbolic link attempt to convert to its real one
				if (File.Exists(device))
				{
					Mono.Unix.UnixSymbolicLinkInfo symLink = new Mono.Unix.UnixSymbolicLinkInfo (device);
					if (symLink.HasContents)
						device = Path.GetFullPath (Path.Combine (Path.GetDirectoryName(device), symLink.ContentsPath));
				}

				// Add to return value
				returnValue.Add (device, mountPath);
			}

			// Return the results
			return returnValue;
		}
		
		/// <summary>
		/// Mount the specified device.
		/// </summary>
		/// <param name='device'>
		/// The device to mount.
		/// </param>
		/// <param name='mountPoint'>
		/// The mount point to use.
		/// </param>
		/// <returns>
		/// Whether or not the mount was successful.
		/// </returns>
		private bool Mount (string device, string mountPoint)
		{
			// Create mount point if it does not exist
			if (!Directory.Exists (mountPoint)) {
				try {
					Directory.CreateDirectory (mountPoint);
				} catch {
					return false;
				}
			}

			// Attempt to mount
			ProcessStartInfo startInfo = new ProcessStartInfo ("/bin/mount", "\"" + device + "\" \"" + mountPoint + "\"");
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;
			Process process = Process.Start (startInfo);
			process.WaitForExit ();
			bool success = process.ExitCode == 0;

			// Remove mount point if mount failed
			if (!success) {
				try
				{
					Directory.Delete (mountPoint);
				}
				catch
				{
				}
			}
			return success;
		}

		/// <summary>
		/// Unmount the specified device.
		/// </summary>
		/// <param name='device'>
		/// The device to unmount.
		/// </param>
		/// <param name='mountPoint'>
		/// The mount point to attempt to remove or null to not touch a mount point.
		/// </param>
		/// <returns>
		/// Whether or not the unmount was successful.
		/// </returns>
		private bool Unmount (string device, string mountPoint)
		{
			// Attempt to unmount
			Process process = null;
			try {
				ProcessStartInfo startInfo = new ProcessStartInfo ("/bin/umount", "\"" + device + "\"");
				startInfo.RedirectStandardError = true;
				startInfo.RedirectStandardOutput = true;
				startInfo.UseShellExecute = false;
				process = Process.Start (startInfo);
				process.WaitForExit ();
			} catch {
				return false;
			}
			if (!string.IsNullOrEmpty (mountPoint))
				try {
					Directory.Delete(mountPoint);
				} catch {
				}
			return process != null && process.ExitCode == 0;
		}
		#endregion
	}
}