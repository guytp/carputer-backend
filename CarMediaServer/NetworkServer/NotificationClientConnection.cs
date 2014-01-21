using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net;

namespace CarMediaServer
{
    /// <summary>
    /// This class represents a single inbound TCP connection to the server for a NotificationNetworkServer.
    /// </summary>
    public class NotificationClientConnection : ClientConnection
    {
        /// <summary>
        /// Create a new client connection.
        /// </summary>
        /// <param name="client">
        /// The underlying TCP client for this connection.
        /// </param>
        public NotificationClientConnection(TcpClient client)
            : base(client)
        {
			Logger.Debug ("New notification client connection");
        }

        /// <summary>
        /// This method provides a single parse of processing to allow the server to process during a loop from the client.
        /// </summary>
        protected override void ProcessSingleLoop(Stream stream)
        {
            // We ignore any inbound data so simply read and empty up the stream
            byte[] buffer = new byte[1024];
            stream.Read(buffer, 0, 1024);
        }

        /// <summary>
        /// Send a notification to this client.
        /// </summary>
        /// <param name="notification">
        /// The notification to send.b
        /// </param>
        public void SendNotification (INetworkNotification notification)
		{
			// Convert the JSON to binary data
			//byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notification, Formatting.Indented));
			byte[] data = notification.SerialiseNotification ();

			try
			{
				// Determine category and code
				byte[] category = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)notification.NotificationCategory));
				byte[] code = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)notification.NotificationCode));


				// Determine length of data as a byte array
				byte[] dataLength = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));

				// Get a handle to a stream
				Stream stream = _tcpClient.GetStream ();

				// Send length of data then data itself
				stream.Write (category, 0, category.Length);
				stream.Write (code, 0, code.Length);
				stream.Write (dataLength, 0, dataLength.Length);
				stream.Write (data, 0, data.Length);
			}
			catch
			{
				if (IsDisconnected)
					return;
				Disconnect();
			}
        }
    }
}