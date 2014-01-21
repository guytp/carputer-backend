using System;

namespace CarMediaServer
{
	/// <summary>
	/// The controller class provides application wide access to core components of the application
	/// that exist as singletons.
	/// </summary>
	public static class Controller
	{
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
	}
}