using System;
using Newtonsoft.Json;

namespace CarMediaServer
{
	public class ArtworkGetResponse
	{
		public bool? ArtistImageAvailable { get; set; }
		public string ArtistImageBase64 { get { return ArtistImage == null ? null : Convert.ToBase64String(ArtistImage); } }
		[JsonIgnore]
		public byte[] ArtistImage { get; set; }

		public bool? AlbumImageAvailable { get; set; }
		public string AlbumImageBase64 { get { return AlbumImage == null ? null : Convert.ToBase64String(AlbumImage); } }
		[JsonIgnore]
		public byte[] AlbumImage { get; set; }


		public ArtworkGetResponse()
		{
		}
	}
}