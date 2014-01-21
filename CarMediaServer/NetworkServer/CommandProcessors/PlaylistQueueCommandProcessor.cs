using System;
using System.Linq;

namespace CarMediaServer
{
    /// <summary>
    /// Defines a command processof for PlaylistQueue commands.
    /// </summary>
    public class PlaylistQueueCommandProcessor : CommandProcessor
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
            if (!(command is PlaylistQueue))
                throw new ArgumentException("Command is not of the correct type");

			// Check this server is in audio mode
			if (!Configuration.AudioSupport)
				throw new NetworkSerialisableException("urn:carputer:audio:notsupported", "This device does not support audio commands", "Configuration.AudioSupport = false", null);
			if (Controller.AudioPlayer == null)
				throw new NetworkSerialisableException("urn:carputer:audio:notsupported", "This device does not support audio playback", "Configuration.AudioSupport = false", null);

			// Validate data
			PlaylistQueue playlistQueueCommand = (PlaylistQueue)command;
			if ((playlistQueueCommand.Request.AudioFileIds == null) || (playlistQueueCommand.Request.AudioFileIds.Length < 1))
				throw new NetworkSerialisableException("urn:carputer:audio:missingfiles", "No audio files were supplied to play/queue", null, null);

			// Get the audio files supplied and throw an error if any are not found
			AudioFile[] audioFiles = AudioFileFactory.ApplicationInstance.ReadAll().Where(audioFile => playlistQueueCommand.Request.AudioFileIds.Contains(audioFile.AudioFileId)).ToArray();
			if (audioFiles.Length != playlistQueueCommand.Request.AudioFileIds.Length)
				throw new NetworkSerialisableException("urn:carputer:audio:invalid", "One or more audio files were not found", null, null);

			// Queue these files in the playlist, optionally clearing first
			if (playlistQueueCommand.Request.ReplaceCurrentQueue)
				Controller.AudioPlayer.PlaylistClear();
			Controller.AudioPlayer.PlaylistAdd(audioFiles);

			// Convert the response to a JSON array and return
			Logger.Debug("PlaylistQueueCommand command queued " + audioFiles.Length + " files");
        	return null;
        }
    }
}