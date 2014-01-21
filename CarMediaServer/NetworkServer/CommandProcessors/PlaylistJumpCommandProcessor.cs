using System;
using System.Linq;

namespace CarMediaServer
{
    /// <summary>
    /// Defines a command processof for PlaylistJumpCommandProcessor commands.
    /// </summary>
    public class PlaylistJumpCommandProcessor : CommandProcessor
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
            if (!(command is PlaylistJump))
                throw new ArgumentException("Command is not a PlaylistJump command");

			// Check this server is in audio mode
			if (!Configuration.AudioSupport)
				throw new NetworkSerialisableException("urn:carputer:audio:notsupported", "This device does not support audio commands", "Configuration.AudioSupport = false", null);

			// Get playlist and ensure we are within bounds
			int newPosition = ((PlaylistJump)command).Position;
			int playlistCount = Controller.AudioPlayer.Playlist.Length;
			if ((newPosition < 0) || (newPosition >= playlistCount))
				throw new NetworkSerialisableException("urn:carputer:audio:playlistjump:outofbounds", "This playlist item you selected is out of bounds", newPosition + " / " + playlistCount, null);

			// Change playlist location and return
			Controller.AudioPlayer.PlayItemInPlaylist(newPosition);
			Logger.Debug("PlaylistJumpCommand completed");
			return null;

        }
    }
}