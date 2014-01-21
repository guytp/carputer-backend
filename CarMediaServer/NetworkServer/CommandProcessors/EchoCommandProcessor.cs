using System;

namespace CarMediaServer
{
    /// <summary>
    /// Defines an echo command processor which is capable of acting upon EchoCommand objects.
    /// </summary>
    public class EchoCommandProcessor : CommandProcessor
    {
        /// <summary>
        /// Process the echo command and return a response identical to the request.
        /// </summary>
        /// <param name="command">
        /// The command to process.
        /// </param>
        /// <returns>
        /// The response to the request.
        /// </returns>
        public override byte[] ProcessCommand(Command command)
        {
            // Check parameters
            if (!(command is EchoCommand))
                throw new ArgumentException("Command is not an echo command");

            // Return the same string in response which in this case is identical to the request
            EchoCommand echo = (EchoCommand)command;
            Logger.Debug("Echo: " + echo.Request.Message);
        	return ToJsonBuffer(echo.Request);
        }
    }
}