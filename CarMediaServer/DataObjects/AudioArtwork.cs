using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace CarMediaServer
{
	/// <summary>
	/// Audio artwork defines an image that can be used for an AudioFile.  Artwork may apply to
	/// an artist or to an artist's album.
	/// </summary>
	[DataObject("AudioArtwork", "Artist ASC, Album ASC", "AudioArtworkId")]
	public class AudioArtwork : DbObject
	{
		/// <summary>
		/// Gets or sets the primary key for this object.
		/// </summary>
        [DataPropertyMapping]
		public int AudioArtworkId { get; set; }

		/// <summary>
		/// Gets or sets the artist that this artwork is for which must always be
		/// supplied.
		/// </summary>
		[DataPropertyMapping]
		public string Artist { get; set; }

		/// <summary>
		/// Gets or sets the optional album that this artwork is for.
		/// </summary>
		[DataPropertyMapping]
		public string Album { get; set; }

		public void Save(Bitmap bitmap)
		{
			if (AudioArtworkId < 1)
				throw new Exception("This object has not been saved");
			if (!Directory.Exists(Configuration.AudioArtworkPath))
				throw new Exception("Unable to save audio artwork as " + Configuration.AudioArtworkPath + " does not exist");

			string filename = Path.Combine(Configuration.AudioArtworkPath, AudioArtworkId + ".png");
			bitmap.Save(filename, ImageFormat.Png);
		}

		public byte[] Load()
		{
			if (AudioArtworkId < 1)
				throw new Exception("This object has not been saved");
			if (!Directory.Exists(Configuration.AudioArtworkPath))
				throw new Exception("Unable to save audio artwork as " + Configuration.AudioArtworkPath + " does not exist");
			
			string filename = Path.Combine(Configuration.AudioArtworkPath, AudioArtworkId + ".png");

			if (!File.Exists(filename))
				return null; // This indicates that we cehecked for content but it wasn't there
			return File.ReadAllBytes(filename);

		}
	}
}

