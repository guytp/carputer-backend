using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace CarMediaServer
{
    /// <summary>
    /// This provides a base class that can be used by TCP/IP network servers to accept inbound connections.
    /// </summary>
    public abstract class TcpServer
    {
        /// <summary>
        /// Gets or sets whether or not the main application should terminate.
        /// </summary>
        public bool ShouldTerminate { get; set; }

        /// <summary>
        /// Defines the listener to receive inbound TCP/IP connections on before handing them off to a thread handler.
        /// </summary>
        private TcpListener _listener;

        /// <summary>
        /// Defines the thread that accepts inbound connections.
        /// </summary>
        private Thread _acceptInboundThread;

        /// <summary>
        /// Defines the thread that handles disconnected clients.
        /// </summary>
        private Thread _reclaimDisconnectThread;

        /// <summary>
        /// Defines the list of currently connected clients.
        /// </summary>
        protected List<ClientConnection> _clients = new List<ClientConnection>();

        /// <summary>
        /// Gets an array of all currently connected clients in a thread-safe fashion.
        /// </summary>
        protected ClientConnection[] Clients
        {
            get
            {
                lock (_clients)
                {
                    return _clients.ToArray();
                }
            }
        }

        /// <summary>
        /// Create a new instance of a TCP server and begin listening for connections.
        /// </summary>
        /// <param name="port">
        /// The port to listen on.
        /// </param>
        public TcpServer(int port)
        {
            // Begin listening for connections
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            // Start a new background thread to accept incoming connections
            string name = GetType().Name;
            _acceptInboundThread = new Thread(new ThreadStart(AcceptInboundConnectionThread))
            {
                Name = name + " Accept Inbound TCP",
                IsBackground = false
            };
            _acceptInboundThread.Start();

            // Start a new thread to handle disconnected clients
            _reclaimDisconnectThread = new Thread(new ThreadStart(ReclaimDisconnectedClientsThread)) {
                Name = name + "Reclaim Disconnected Clients",
                IsBackground = false
            };
            _reclaimDisconnectThread.Start();
        }

        /// <summary>
        /// This method is the main entry for a thread that accepts TCP connections then hands them off to a ClientConnection
        /// that interacts with the TCP stream.
        /// </summary>
        private void AcceptInboundConnectionThread()
        {
            while (!ShouldTerminate)
            {
                TcpClient client = _listener.AcceptTcpClient();
                lock (_clients)
                {
                    _clients.Add(CreateClientConnection(client));
                }
            }
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
        protected abstract ClientConnection CreateClientConnection(TcpClient tcpClient);

        /// <summary>
        /// This method provides the entry point for a thread that looks for ClientConnections that have become
        /// disconncted and then removes them from the server.
        /// </summary>
        private void ReclaimDisconnectedClientsThread()
        {
            while (!ShouldTerminate)
            {
                lock (_clients)
                {
                    for (int i = _clients.Count - 1; i >= 0; i--)
                    {
                        ClientConnection client = _clients[i];
                        if (!client.IsAlive)
                            _clients.Remove(client);
                    }
                }
                Thread.Sleep(5000);
            }
        }
    }
}