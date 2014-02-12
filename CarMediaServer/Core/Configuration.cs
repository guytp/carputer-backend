using System;

namespace CarMediaServer
{
	public static class Configuration
	{
		public static string ConnectionString { get; set; }

		public static ushort NetworkQueryListenPort { get; set; }

		public static ushort NotificationListenPort { get; set; }

		public static bool AudioSupport { get { return Controller.AudioFileDiscoverer != null; } }

		public static string AudioArtworkPath { get; set; }

		public static string LastFmKey { get; set; }

		public static string LastFmSecret { get; set; }
	}
}