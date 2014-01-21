using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CarMediaServer
{
    /// <summary>
    /// This class represents a base abstract class to be used for inbound TCP connection to any TcpServer.
    /// </summary>
    public abstract class ClientConnection
    {
		/// <summary>
		/// Gets whether or not a call to Disconnect has been made.
		/// </summary>
		public bool IsDisconnected { get; private set; }

        /// <summary>
        /// Defines the TCP client in use for this connection.
        /// </summary>
        protected TcpClient _tcpClient;

        /// <summary>
        /// Defines the main processing thread.
        /// </summary>
        private Thread _processingThread;

        /// <summary>
        /// Gets whether or not the client is alive.
        /// </summary>
        public bool IsAlive { get; private set; }

        /// <summary>
        /// Create a new client connection.
        /// </summary>
        /// <param name="client">
        /// The underlying TCP client for this connection.
        /// </param>
        public ClientConnection(TcpClient client)
        {
            // Store the client
            _tcpClient = client;

            // Start processing the inbound client
            IsAlive = true;
            _processingThread = new Thread(new ThreadStart(ProcessingThread))
            {
                Name = "Client Processing " + GetType().Name,
                IsBackground = false
            };
            _processingThread.Start();
        }

        /// <summary>
        /// This is the main thread that processes all inbound activities, parses commands and then hands off to the appropriate processor.
        /// </summary>
        private void ProcessingThread()
        {
            Stream stream = _tcpClient.GetStream();
            while (IsAlive)
            {
                
                try
                {
                    ProcessSingleLoop(stream);
                }
                catch (Exception ex)
                {
					if (IsDisconnected)
						return;
                    string forciblyTerminated = "An existing connection was forcibly closed by the remote host";
                    if (!((ex is IOException) && (ex.InnerException != null) && (ex.InnerException is SocketException) && (ex.InnerException.Message == forciblyTerminated)))
                        Logger.Error("Disconnecting for exception", ex);
                    Disconnect();
                    return;
                }
            }
        }
        
        /// <summary>
        /// This method provides a single parse of processing to allow the server to process during a loop from the client.
        /// </summary>
        protected abstract void ProcessSingleLoop(Stream stream);

        /// <summary>
        /// Disconnect the client gracefully closing its underlying connection and then marking it as no longer alive.
        /// </summary>
        public void Disconnect()
        {
			// Return if already disconnected
			if (IsDisconnected)
				return;

			IsDisconnected = true;
            try
            {
                _tcpClient.Close();
            }
            catch
            {
                // Swallow errors whilst closing
            }
			_tcpClient = null;
            IsAlive = false;
            Logger.Info("Client " + GetType().Name + ":" + GetHashCode() + " disconnected");
        }
    }
}