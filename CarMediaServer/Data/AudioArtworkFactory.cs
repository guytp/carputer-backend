using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace CarMediaServer
{
    /// <summary>
    /// This class provides access to the database for AudioArtworkFactory objects.
    /// </summary>
    public class AudioArtworkFactory : Factory<AudioArtwork, Int32>
    {
        /// <summary>
        /// Gets a handle to a one-off instantiated instance of the AudioFileFactory.  This should be used for non-thread
        /// safe related database calls where there is no need for creating your own factory.
        /// </summary>
        public static AudioArtworkFactory ApplicationInstance { get; private set; }

        /// <summary>
        /// One off static construction of this factory class.
        /// </summary>
        static AudioArtworkFactory()
        {
            ApplicationInstance = new AudioArtworkFactory();
        }

		public AudioArtwork GetForArtist(string artist)
		{
			return ReadAll().FirstOrDefault(audioArtwork => audioArtwork.Artist.ToLower() == artist.ToLower() && string.IsNullOrEmpty(audioArtwork.Album));
		}

		public AudioArtwork GetForAlbum(string artist, string album)
		{
			return ReadAll().FirstOrDefault(audioArtwork => audioArtwork.Artist.ToLower() == artist.ToLower() && !string.IsNullOrEmpty(audioArtwork.Album) && audioArtwork.Album.ToLower() == album.ToLower());
		}
    }
}