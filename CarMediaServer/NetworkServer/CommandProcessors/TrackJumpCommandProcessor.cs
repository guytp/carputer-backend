using System;
using System.Linq;

namespace CarMediaServer
{
    /// <summary>
    /// Defines a command processof for TrackJumpCommandProcessor commands.
    /// </summary>
    public class TrackJumpCommandProcessor : CommandProcessor
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
            if (!(command is TrackJump))
                throw new ArgumentException("Command is not a TrackJump command");

			// Check this server is in audio mode
			if (!Configuration.AudioSupport)
				throw new NetworkSerialisableException("urn:carputer:audio:notsupported", "This device does not support audio commands", "Configuration.AudioSupport = false", null);

			// Update position in the track
			Controller.AudioPlayer.JumpToSecondsOffset(((TrackJump)command).Offset);
			Logger.Debug("TrackJumpCommand completed");
			return null;
        }
    }
}