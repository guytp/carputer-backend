using System;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace CarMediaServer
{
	class MainClass

	{

		public static void Main(string[] args)
		{
			try
			{
				AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
				// Setup configuration
				Configuration.AudioArtworkPath = "/home/guytp/AudioArtwork";
				if (!Directory.Exists(Configuration.AudioArtworkPath))
					Directory.CreateDirectory(Configuration.AudioArtworkPath);
				Configuration.ConnectionString = "server=localhost;user id=root; password=sjq92jha; database=carputer; pooling=false; charset=utf8;";
				Configuration.NetworkQueryListenPort = 4200;
				Configuration.NotificationListenPort = 4201;
				
				Configuration.LastFmSecret = "dbab3e9ec63f2c599afe766fd69b55c1";
				Configuration.LastFmKey = "587720b4736299afcc534383f2d5ba5e";

				// Create new TCP server and wait for it to terminate gracefully
				Controller.CommandNetworkServer = new CommandNetworkServer();
				Controller.NotificationNetworkServer = new NotificationNetworkServer();

				// Start the audio sub-system
				Controller.AudioFileDiscoverer = new AudioFileDiscoverer();
				Controller.AudioPlayer = new AudioPlayer();
				Controller.AudioArtworkDiscoverer = new AudioArtworkDiscoverer();

				// Start discovery broadcaster
				DiscoveryBroadcaster discoveryBroadcaster = new DiscoveryBroadcaster(Configuration.AudioSupport, Configuration.NetworkQueryListenPort, Configuration.NotificationListenPort);
				discoveryBroadcaster.Start();

				// Run indefinitely
				while (true)
					Thread.Sleep(500);
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		}

		static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			HandleException(e.ExceptionObject as Exception);
		}

		static void HandleException(Exception ex)
		{
			try
			{
				Logger.Error("Fatal exception caused application to terminate thread, re-starting Carputer", ex);
			}
			catch
			{
			}

			string location = Assembly.GetEntryAssembly().Location;
			Process.Start(location);
			Thread.Sleep(500);
			Environment.Exit(-1);
		}
	}
}