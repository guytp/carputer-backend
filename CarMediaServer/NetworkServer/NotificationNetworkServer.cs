using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace CarMediaServer
{
    /// <summary>
    /// The NotificationNetworkServer is a concrete implementation of TcpServer that provides a TCP/IP connection
    /// for outbound notification messages to clients.
    /// </summary>
    public class NotificationNetworkServer : TcpServer
    {
		private readonly List<INetworkNotification> _notificationQueue = new List<INetworkNotification>();

        /// <summary>
        /// Create a new instance of the server.
        /// </summary>
        public NotificationNetworkServer()
            : base(Configuration.NotificationListenPort)
        {
			new Thread(NotificationQueueThread) { IsBackground = true, Name = "Notification Queue Thread" }.Start();
        }

        /// <summary>
        /// Creates a new ClientConnection for this TCP Server from the raw TcpClient.
        /// </summary>
        /// <param name="tcpClient">
        /// The TcpClient to use to creat the clint connection.
        /// </param>
        /// <returns>
        /// An instance of a ClientConnection.
        /// </returns>
        protected override ClientConnection CreateClientConnection(TcpClient tcpClient)
        {
            return new NotificationClientConnection(tcpClient);
        }

        /// <summary>
        /// Sends a notification to all connected clients.
        /// </summary>
        /// <param name="notification">
        /// The notification to send.
        /// </param>
        public void SendNotification (INetworkNotification notification)
		{
			lock (_notificationQueue)
				_notificationQueue.Add (notification);
		}

		private void NotificationQueueThread()
		{
			while (true)
			{
				INetworkNotification[] notifications;
				lock (_notificationQueue)
				{
					notifications = _notificationQueue.ToArray();
					_notificationQueue.Clear();
				}
				foreach (INetworkNotification notification in notifications)
				{
					try
					{
						List<Exception> exceptions = new List<Exception> ();
						ClientConnection[] clients = Clients;
						Logger.Debug ("Sending notification " + notification.GetType().Name + " to all clients");
						foreach (ClientConnection client in clients)
							try {
								Logger.Debug ("Sending notification " + notification.GetType ().Name + " to client " + client.GetHashCode ()); 
								((NotificationClientConnection)client).SendNotification (notification);
							} catch (Exception ex) {
								exceptions.Add (ex);
							}
						if (exceptions.Count > 0)
							throw new AggregateException (exceptions.ToArray ());
					}
					catch
					{
					}
				}

				Thread.Sleep(50);
			}
        }
    }
}