using System;
using System.Threading;

namespace CarMediaServer
{
	class MainClass
	{

		public static void Main (string[] args)
		{
			// Setup configuration
			Configuration.ConnectionString = "server=localhost;user id=root; password=sjq92jha; database=carputer; pooling=false";
			Configuration.NetworkQueryListenPort = 4200;
			Configuration.NotificationListenPort = 4201;

			// Create new TCP server and wait for it to terminate gracefully
            Controller.CommandNetworkServer = new CommandNetworkServer();
            Controller.NotificationNetworkServer = new NotificationNetworkServer();

            // Start the audio sub-system
			Controller.AudioFileDiscoverer = new AudioFileDiscoverer();
			Controller.AudioPlayer = new AudioPlayer();

			// Start discovery broadcaster
			DiscoveryBroadcaster discoveryBroadcaster = new DiscoveryBroadcaster(Configuration.AudioSupport, Configuration.NetworkQueryListenPort, Configuration.NotificationListenPort);
			discoveryBroadcaster.Start();

            // Run indefinitely
            while (true)
                Thread.Sleep(500);
		}
	}
}