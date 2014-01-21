using System;
using System.Linq;

namespace CarMediaServer
{
    /// <summary>
    /// Defines a command processof for ToggleRepeat commands.
    /// </summary>
    public class ToggleRepeatCommandProcessor : CommandProcessor
    {
        /// <summary>
        /// Process the command.
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
            if (!(command is EmptyCommand))
                throw new ArgumentException("Command is not a EmptyCommand command");

			// Change playlist location and return
			Controller.AudioPlayer.ToggleRepeat();
			Logger.Debug("ToggleRepeat completed");
			return null;

        }
    }
}