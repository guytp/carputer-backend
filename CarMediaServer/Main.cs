using System;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using MySql.Data.MySqlClient;

namespace CarMediaServer
{
	class MainClass

	{

		public static void Main(string[] args)
		{
			Configuration.ConnectionString = "server=localhost;user id=root; password=sjq92jha; database=carputer; pooling=false; charset=utf8;";
			try
			{
				// Before we do anythign wait for DB server
				while (true)
				{
					try
					{
	                	Console.WriteLine("Waiting for DB server to wake up...");
	                	using (MySqlConnection conn = new MySqlConnection(Configuration.ConnectionString))
						{
			                conn.Open();
	        		        using (MySqlCommand command = new MySqlCommand("SELECT 1", conn))
							{
								command.ExecuteScalar();
							}
							break;
						}
					}
					catch
					{
						Console.WriteLine("Error whilst querying, waiting to try again...");
						Thread.Sleep(1000);
					}
				}


				AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

				// Setup our volume to be 100%
				string temporaryFile = Path.GetTempFileName();
				string fileContents = "#!/bin/bash" + Environment.NewLine + "export mixer=`amixer | head -n1 | sed -r \"s/.*(control ')(.*)'(.*)/\\2/\"`" + Environment.NewLine + "amixer set \"$mixer\" playback volume 100% > /dev/null";
				File.WriteAllText(temporaryFile, fileContents);
				ProcessStartInfo startInfo = new ProcessStartInfo();
				startInfo.FileName = "/bin/bash";
				startInfo.Arguments = temporaryFile;
				startInfo.UseShellExecute = false;
				Process.Start(startInfo).WaitForExit();
				File.Delete(temporaryFile);

				// Setup configuration
				Configuration.AudioArtworkPath = "/home/guytp/AudioArtwork";
				if (!Directory.Exists(Configuration.AudioArtworkPath))
					Directory.CreateDirectory(Configuration.AudioArtworkPath);
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
				//DiscoveryBroadcaster discoveryBroadcaster = new DiscoveryBroadcaster(Configuration.AudioSupport, Configuration.NetworkQueryListenPort, Configuration.NotificationListenPort);
				//discoveryBroadcaster.Start();

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