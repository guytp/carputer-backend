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
    /// This class represents a single inbound TCP connection to the server for a CommandNetworkServer.
    /// </summary>
    public class CommandClientConnection : ClientConnection
    {
        /// <summary>
        /// Defines a dictionary containing command processors keyed by the command category and containing a dictionary keyed by opcode and type of processor.
        /// </summary>
        private static Dictionary<ushort, Dictionary<ushort, Type>> _commandProcessors;

        /// <summary>
        /// Static constructor for one-time setup of the class.
        /// </summary>
        static CommandClientConnection()
        {
            _commandProcessors = CommandProcessor.GetProcessors();
        }

        /// <summary>
        /// Create a new client connection.
        /// </summary>
        /// <param name="client">
        /// The underlying TCP client for this connection.
        /// </param>
        public CommandClientConnection(TcpClient client)
            : base(client)
        {
			Logger.Debug ("New command client connection");
        }

        /// <summary>
        /// This method provides a single parse of processing to allow the server to process during a loop from the client.
        /// </summary>
        protected override void ProcessSingleLoop (Stream stream)
		{
			byte[] headerBuffer = new byte[8];
			// Attempt to read 8 bytes for the header.  If there aren't 8 bytes waiting then the client is not communicating in an expeted manor and we disconnect it.
			Logger.Debug ("[Command Client] ProcessSingleLoop() Start, reading data");
			int bytesRead = stream.Read (headerBuffer, 0, 8);
			Logger.Debug ("[Command Client] ProcessSingleLoop() data read");
            if (bytesRead != 8)
            {
				if (IsDisconnected)
					return;
                Disconnect();
                return;
            }
            Logger.Debug("[Command Client] ProcessSingleLoop() Read " + bytesRead + " bytes");

            // Determine the command category, opcode and the data length
            Logger.Debug("[Command Client] ProcessSingleLoop() Determining category, opcode and data length");
            ushort commandCategory = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(new byte[] { headerBuffer[0], headerBuffer[1] }, 0));
            ushort opCode = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(new byte[] { headerBuffer[2], headerBuffer[3] }, 0));
            int dataLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(new byte[] { headerBuffer[4], headerBuffer[5], headerBuffer[6], headerBuffer[7] }, 0));
            Logger.Debug("[Command Client] ProcessSingleLoop() Category, opcode and data length values read");

            // If data is supplied keep reading until we've got all data from the device
            string data = null;
            if (dataLength > 0)
            {
                Logger.Debug("[Command Client] ProcessSingleLoop() Reading data");
                int dataToRead = dataLength;
                int bufferOffset = 0;
                byte[] dataBuffer = new byte[dataToRead];
                while (dataToRead > 0)
                {
                    bytesRead = stream.Read(dataBuffer, bufferOffset, dataToRead);
                    dataToRead -= bytesRead;
                    bufferOffset += bytesRead;
                }
                Logger.Debug("[Command Client] ProcessSingleLoop() Data read, converting to string");
                data = Encoding.UTF8.GetString(dataBuffer);
                Logger.Debug("[Command Client] ProcessSingleLoop() Converted data to string");
            }

            // Create an instance for this command
            byte[] response = null;
            bool exceptionOccurred = false;
            Command command = null;
            string commandName = null;
            try
            {
                Logger.Debug("[Command Client] ProcessSingleLoop() Creating command for " + commandCategory + " / " + opCode);
                command = Command.CreateCommand(commandCategory, opCode, data);
                if (command == null)
                {
                    Logger.Error("[Command Client] ProcessSingleLoop() Command not of a known type " + commandCategory + " / " + opCode);
                    return;
                }
                commandName = command.GetType().Name;
                Logger.Debug("[Command Client] ProcessSingleLoop() Created command");

                // Hand off to a processor
                Logger.Debug("[Command Client] ProcessSingleLoop() Determining processor for category");
                if (!_commandProcessors.ContainsKey(commandCategory))
                {
                    Logger.Error("[Command Client] ProcessSingleLoop() No command processor for category " + commandCategory + " (" + commandName + ")");
                    return;
                }
                Logger.Debug("[Command Client] ProcessSingleLoop() Determining processor for opcode");
                Dictionary<ushort, Type> processors = _commandProcessors[commandCategory];
                if (!processors.ContainsKey(opCode))
                {
                    Logger.Error("[Command Client] ProcessSingleLoop() No command processor for opcode " + commandCategory + " / " + opCode + " (" + commandName + ")");
                    return;
                }
                Logger.Debug("[Command Client] Creating processor for " + commandName);
                CommandProcessor processor = (CommandProcessor)Activator.CreateInstance(processors[opCode]);
                Logger.Debug("[Command Client] Processor created for " + commandName);
                Logger.Debug("[Command Client] ProcessSingleLoop() Starting command " + commandName);
                response = processor.ProcessCommand(command);
                Logger.Debug("[Command Client] ProcessSingleLoop() Finished command " + commandName);
            }
            catch (Exception ex)
            {
                NetworkErrorMessage errorMessage = ex is INetworkSerialisableException ? ((INetworkSerialisableException)ex).NetworkErrorMessage : new NetworkErrorMessage(ex);
                response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(errorMessage, Formatting.Indented));
                exceptionOccurred = true;
                if (command == null)
                    Logger.Error("[Command Client] ProcessSingleLoop() exception parsing inbound command", ex);
                else
                    Logger.Error("[Command Client] ProcessSingleLoop() Exception processing " + commandName, ex);
            }

            // If there was a response write to to the client otherwise inform null response
            if ((response == null) || (response.Length == 0))
            {
                Logger.Debug("[Command Client] ProcessSingleLoop() Sending null response for " + commandName);
                stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(0)), 0, 4);
                Logger.Debug("[Command Client] ProcessSingleLoop() Finished sending null response for " + commandName);
            }
            else
            {
                Logger.Debug("[Command Client] ProcessSingleLoop() Sending response for " + commandName);
                byte[] responseBuffer = new byte[4 + response.Length];
                // If an error has occurred we indicate this with a negative length
                byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(exceptionOccurred ? response.Length * -1 : response.Length));
                Array.Copy(length, responseBuffer, 4);
                Array.Copy(response, 0, responseBuffer, 4, response.Length);
                stream.Write(responseBuffer, 0, responseBuffer.Length);
                Logger.Debug("[Command Client] ProcessSingleLoop() Finished sending response for " + commandName);
            }
            Logger.Debug("[Command Client] ProcessSingleLoop() Ends");
        }
    }
}