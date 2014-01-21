using System;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net;

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
		#endregion

		#region Constructors
		/// <summary>
		/// Create a new instance of this class.
		/// </summary>
		public DiscoveryBroadcaster (bool audioSupport, ushort commandPort, ushort notificationPort)
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
					// Create JSON for broadcast
					byte[] json = System.Text.Encoding.UTF8.GetBytes (JsonConvert.SerializeObject (this));

					// Loop as long as we're running
					while (_isThreadRunning) {
						// Create the socket
						Socket s = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
						IPAddress ip = IPAddress.Parse ("239.42.0.1");
						s.SetSocketOption (SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption (ip));
						s.SetSocketOption (SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
						IPEndPoint ipep = new IPEndPoint (ip, 4200);
						s.Connect (ipep);

						// Write to socket
						s.Send (json, json.Length, SocketFlags.None);

						// Close socket
						s.Close ();
						s.Dispose ();

						// Wait for next loop
						Thread.Sleep (1000);
					}
					if (!_isThreadRunning)
						break;
				} catch (Exception ex) {
					System.Diagnostics.Trace.WriteLine (ex.ToString ());
				}
			}

			// Mark as no longer running
			_broadcastThread = null;
		}
		#endregion
	}
}