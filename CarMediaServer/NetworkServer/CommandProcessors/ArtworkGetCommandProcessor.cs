using System;

namespace CarMediaServer
{
    /// <summary>
    /// Defines a command processor that is able to retrieve album artwork.
    /// </summary>
    public class ArtworkGetCommandProcessor : CommandProcessor
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
			if (!(command is ArtworkGet))
				throw new ArgumentException("Command is not an artwork get command");
			ArtworkGet request = command as ArtworkGet;

			// Create an empty response object
			ArtworkGetResponse response = new ArtworkGetResponse();

			// Try to retrieve artist image if requested
			if ((!string.IsNullOrEmpty(request.Artist)) && (request.GetArtistImage))
			{
				AudioArtwork artwork = AudioArtworkFactory.ApplicationInstance.GetForArtist(request.Artist);
				if (artwork != null)
				{
					response.ArtistImage = artwork.Load();
					response.ArtistImageAvailable = response.ArtistImage != null;
				}
			}

			// Try to retrieve album image if request
			if ((!string.IsNullOrEmpty(request.Artist)) && (!string.IsNullOrEmpty(request.Album)) && (request.GetAlbumImage))
			{
				AudioArtwork artwork = AudioArtworkFactory.ApplicationInstance.GetForAlbum(request.Artist, request.Album);
				if (artwork != null)
				{
					response.AlbumImage = artwork.Load();
					response.AlbumImageAvailable = response.AlbumImage != null;
				}
			}

			return ToJsonBuffer(response);
		}
    }
}