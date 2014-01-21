﻿using System;
using System.Linq;

namespace CarMediaServer
{
    /// <summary>
    /// Defines a command processof for PauseToggleCommand commands.
    /// </summary>
    public class PauseToggleCommandProcessor : CommandProcessor
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
			Controller.AudioPlayer.TogglePlayPause();
			Logger.Debug("PauseToggle completed");
			return null;

        }
    }
}