using System;
using System.Linq;

namespace CarMediaServer
{
    /// <summary>
    /// Defines a command processof for AudioLibraryGet commands.
    /// </summary>
    public class AudioLibraryGetCommandProcessor : CommandProcessor
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
                throw new ArgumentException("Command is not an echo command");

			// Check this server is in audio mode
			if (!Configuration.AudioSupport)
				throw new NetworkSerialisableException("urn:carputer:audio:notsupported", "This device does not support audio commands", "Configuration.AudioSupport = false", null);

			// Get the current list of audio files for any active devices
			string[] uuids = Controller.AudioFileDiscoverer.MountedDeviceUuids;
			Logger.Debug("AudioLibraryGet command found " + uuids.Length + " connected devices");
			AudioFile[] files = null;
			if (uuids != null)
				files = AudioFileFactory.ApplicationInstance.ReadAll().Where(file => uuids.Contains(file.DeviceUuid)).ToArray();
			if (files == null)
				files = new AudioFile[0];

			// Convert the response to a JSON array and return
			Logger.Debug("AudioLibraryGet command returning " + files.Length + " files");
        	return ToJsonBuffer(files);

        }
    }
}