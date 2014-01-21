using System;
using System.Net.Sockets;

namespace CarMediaServer
{
    /// <summary>
    /// The CommandNetworkServer is a concrete implementation of TcpServer that provides a TCP/IP connection
    /// for inbound command requests from clients.
    /// </summary>
    public class CommandNetworkServer : TcpServer
    {
        /// <summary>
        /// Create a new instance of the server.
        /// </summary>
        public CommandNetworkServer()
            : base(Configuration.NetworkQueryListenPort)
        {
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
            return new CommandClientConnection(tcpClient);
        }
    }
}