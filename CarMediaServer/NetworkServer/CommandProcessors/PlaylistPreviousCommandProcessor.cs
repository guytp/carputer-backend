using System;
using System.Linq;

namespace CarMediaServer
{
    /// <summary>
    /// Defines a command processof for PlaylistPrevious commands.
    /// </summary>
    public class PlaylistPreviousCommandProcessor : CommandProcessor
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
			if ((Controller.AudioPlayer.IsPlaying) && (Controller.AudioPlayer.Progress >= 5))
				Controller.AudioPlayer.JumpToSecondsOffset(0);
			else
				Controller.AudioPlayer.PlayPreviousInPlaylist();
			Logger.Debug("PlaylistPrevious completed");
			return null;

        }
    }
}