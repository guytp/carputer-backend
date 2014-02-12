using System;
using System.IO;

namespace CarMediaServer
{
	/// <summary>
	/// The controller class provides application wide access to core components of the application
	/// that exist as singletons.
	/// </summary>
	public static class Controller
	{
		/// <summary>
		/// Defines the serial number of the device.
		/// </summary>
		private static string _serialNumber;

		/// <summary>
		/// Gets or sets the application-wide instance of the command network server.
		/// </summary>
		public static CommandNetworkServer CommandNetworkServer { get; set; }
		
		/// <summary>
		/// Gets or sets the application-wide instance of the notification network server.
		/// </summary>
		public static NotificationNetworkServer NotificationNetworkServer { get; set; }

		/// <summary>
		/// Gets or sets the application-wide instance of the audio file discoverer.
		/// </summary>
		public static AudioFileDiscoverer AudioFileDiscoverer { get; set; }
		
		/// <summary>
		/// Gets or sets the application-wide instance of the audio player.
		/// </summary>
		public static AudioPlayer AudioPlayer { get; set; }

		/// <summary>
		/// Gets or sets the application-wide instance of the mount manager.
		/// </summary>
		public static MountManager MountManager { get; set; }

		public static AudioArtworkDiscoverer AudioArtworkDiscoverer { get; set; }

		/// <summary>
		/// Gets the serial number of the device.
		/// </summary>
		public static string SerialNumber
		{
			get
			{
				if (!string.IsNullOrEmpty(_serialNumber))
					return _serialNumber;

				// Attempt to read from value stored on disk
				const string filename = "/etc/carputer/serialnumber";
				if (File.Exists(filename))
				{
					string[] lines = File.ReadAllLines(filename);
					if (lines.Length > 0)
					{
						if (!string.IsNullOrWhiteSpace(lines[0]))
						{
							_serialNumber = lines[0];
							return _serialNumber;
						}
					}
				}

				// We need to create a new serial number and store it to disk.  If this fails we should
				// throw an exception
				if (!Directory.Exists(Path.GetDirectoryName(filename)))
					Directory.CreateDirectory(Path.GetDirectoryName(filename));
				_serialNumber = Guid.NewGuid().ToString().Replace("-", "").ToLower();
				File.WriteAllText(filename, _serialNumber);
				return _serialNumber;
			}
		}
	}
}