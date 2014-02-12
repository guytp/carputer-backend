using System;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CarMediaServer
{
	/// <summary>
	/// This class broadcasts information about the server over a multicast address.
	/// </summary>
	public class DiscoveryBroadcaster
	{
		#region Declarations
		/// <summary>
		/// Defines whether or not the broadcast thread should be running.
		/// </summary>
		private bool _isThreadRunning = false;

		/// <summary>
		/// Defines the thread to broadcast discovery information.
		/// </summary>
		private Thread _broadcastThread;
		#endregion

		#region Properties
		/// <summary>
		/// Gets whether or not this server supports audio content and playback.
		/// </summary>
		public bool AudioSupport { get; private set; }
		
		/// <summary>
		/// Gets the command port used by this server.
		/// </summary>
		public ushort CommandPort { get; private set; }
		
		/// <summary>
		/// Gets the notification port used by this server.
		/// </summary>
		public ushort NotificationPort { get; private set; }

		/// <summary>
		/// Gets the hostname of this machine.
		/// </summary>
		public string Hostname { get { return Environment.MachineName; } }

		/// <summary>
		/// Gets the serial number of this machine.
		/// </summary>
		public string SerialNumber { get { return Controller.SerialNumber; } }
		#endregion

		#region Constructors
		/// <summary>
		/// Create a new instance of this class.
		/// </summary>
		public DiscoveryBroadcaster(bool audioSupport, ushort commandPort, ushort notificationPort)
		{
			AudioSupport = audioSupport;
			CommandPort = commandPort;
			NotificationPort = notificationPort;
		}
		#endregion

		#region Public Control
		/// <summary>
		/// Start the discovery broadcaster.
		/// </summary>
		public void Start()
		{
			// Return if already broadcast
			if (_isThreadRunning)
				return;

			// Start broadcast thread
			_broadcastThread = new Thread(BroadcastThread) { Name = "Discovery Broadcaster", IsBackground = true };
			_isThreadRunning = true;
			_broadcastThread.Start();
		}

		/// <summary>
		/// Stop the discovery broadcaster.
		/// </summary>
		public void Stop()
		{
			// Try to kill thread
			_isThreadRunning = false;
			DateTime endTime = DateTime.UtcNow.AddSeconds (5);
			while ((_broadcastThread != null) && (_broadcastThread.ThreadState == ThreadState.Running) && (DateTime.UtcNow < endTime))
				Thread.Sleep (50);
			if ((_broadcastThread != null) && (_broadcastThread.ThreadState == ThreadState.Running))
				try
				{
					_broadcastThread.Abort();
				}
				catch
				{
				}
			_broadcastThread = null;
		}
		#endregion

		#region Threads
		/// <summary>
		/// This thread defines the main method used to publicise information about this host.
		/// </summary>
		private void BroadcastThread ()
		{
			while (true) {
				try {
					// Get our broadcast address - if we don't have one, wait and try again
					IPAddress broadcast = GetPrimaryBroadcast();
					if (broadcast == null)
					{
						Thread.Sleep(100);
						continue;
					}

					// Create JSON for broadcast
					byte[] json = System.Text.Encoding.UTF8.GetBytes (JsonConvert.SerializeObject (this));

					// Loop as long as we're running
					using (UdpClient client = new UdpClient())
					{
						IPEndPoint ip = new IPEndPoint(broadcast, 4200);
						client.EnableBroadcast = true;
						client.DontFragment = true;
						client.Send(json, json.Length, ip);
					}
				} catch (Exception ex) {
					Console.WriteLine("Send exception in broadcast"+ ex);
					System.Diagnostics.Trace.WriteLine (ex.ToString ());
				}
				if (!_isThreadRunning)
					break;

				// Wait for next loop
				DateTime endTime = DateTime.UtcNow.AddSeconds(0.5);
				while (DateTime.UtcNow < endTime)
					Thread.Sleep (10);
			}

			// Mark as no longer running
			_broadcastThread = null;
		}
		#endregion

		private static IPAddress GetPrimaryBroadcast()
		{
			foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (netInterface.OperationalStatus != OperationalStatus.Up)
					continue;
				IPInterfaceProperties ipProps = netInterface.GetIPProperties();
				foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
				{
					if ((addr.Address.AddressFamily == AddressFamily.InterNetwork) && (addr.Address.ToString() != "127.0.0.1"))
					{
						byte[] ipBytes = addr.Address.GetAddressBytes();
						byte[] maskBytes = IPInfoTools.GetIPv4Mask(netInterface.Name).GetAddressBytes();
					    byte[] broadcastIPBytes = new byte[4];
					    for (int i = 0; i < 4; i++)
					    {
					        byte inverseByte = (byte)~ maskBytes[i];
					        broadcastIPBytes[i] = (byte)(ipBytes[i] | inverseByte);
					    }
					    return new IPAddress(broadcastIPBytes);
					}
				}
			}
			return null;
		}
	}
}