using System;
using Newtonsoft.Json;

namespace CarMediaServer
{
	/// <summary>
	/// This command gets artwork for either an artist, album or both
	/// </summary>
	public class ArtworkGet : AudioCommand
	{
		#region Properties
		/// <summary>
		/// Gets or sets the artist of the artwork request.
		/// </summary>
		public string Artist { get; set; }
		
		/// <summary>
		/// Gets or sets the optional album of the artwork request.
		/// </summary>
		public string Album { get; set; }
		
		/// <summary>
		/// Gets or sets whether or not to return the artist image.
		/// </summary>
		public bool GetArtistImage { get; set; }
		
		/// <summary>
		/// Gets or sets whether or not to return the album image.
		/// </summary>
		public bool GetAlbumImage { get; set; }
		#endregion

		#region Constructors
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
		public ArtworkGet()
		{
		}

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="json">
        /// The raw JSON that is passed to this command.
        /// </param>
        public ArtworkGet(string json)
        {
			ArtworkGet request = JsonConvert.DeserializeObject<ArtworkGet>(json);
			Artist = request.Artist;
			Album = request.Album;
			GetArtistImage = request.GetArtistImage;
			GetAlbumImage = request.GetAlbumImage;
		}
		#endregion
	}
}