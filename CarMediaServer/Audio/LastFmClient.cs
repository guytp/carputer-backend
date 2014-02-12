using System;
using System.Web;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Drawing;

namespace CarMediaServer
{
	public class LastFmClient
	{
		public Bitmap GetAlbumImage(string artist, string album)
		{
			// First we make a request for artist information which contains links to images
			string encodedArtist = Uri.EscapeDataString(artist);
			string encodedAlbum = Uri.EscapeDataString(album);
			string url = "http://ws.audioscrobbler.com/2.0/?method=album.getinfo&artist=" + encodedArtist + "&api_key=" + Configuration.LastFmKey + "&album=" + encodedAlbum;

			// Perform a general API request
			return CallApiForImages(url, "/lfm/album/image");
		}

		public Bitmap GetArtistImage(string artist)
		{					
			// First we make a request for artist information which contains links to images
			string encodedArtist = Uri.EscapeDataString(artist);
			string url = "http://ws.audioscrobbler.com/2.0/?method=artist.getinfo&artist=" + encodedArtist + "&api_key=" + Configuration.LastFmKey;

			// Perform a general API request
			return CallApiForImages(url, "/lfm/artist/image");
		}

		private Bitmap CallApiForImages(string url, string imageNodePath)
		{
			Dictionary<string, string> imageFiles = new Dictionary<string, string>();
			using (WebClient client = new WebClient())
			{
				Logger.Debug("Requesting last.fm info from: " + url);
				using (Stream readStream = client.OpenRead(url))
				{
					using (StreamReader reader = new StreamReader(readStream))
					{
						string xml = reader.ReadToEnd();
						XmlDocument doc = new XmlDocument();
						doc.LoadXml(xml);
						XmlNodeList imageNodes = doc.SelectNodes(imageNodePath);
						foreach (XmlNode node in imageNodes)
						{
							XmlAttribute sizeAttribute = node.Attributes["size"];
							string xmlUrl = node.InnerText;
							string size = sizeAttribute == null ? null : sizeAttribute.Value;
							if (!imageFiles.ContainsKey(size))
								imageFiles.Add(size, xmlUrl);
							else
								imageFiles[size] = xmlUrl;
						}
					}
				}

				// Determine the largest size image we can
				string imageUrl = null;
				if (imageFiles.ContainsKey("mega"))
					imageUrl = imageFiles["mega"];
				else if (imageFiles.ContainsKey("extralarge"))
					imageUrl = imageFiles["extralarge"];
				else if (imageFiles.ContainsKey("large"))
					imageUrl = imageFiles["large"];
				else if (imageFiles.ContainsKey("medium"))
					imageUrl = imageFiles["medium"];
				else if (imageFiles.ContainsKey("small"))
					imageUrl = imageFiles["small"];
				if (imageUrl.EndsWith("keepstatsclean.jpg"))
				{
					Logger.Debug("Last.fm gave us a keepstatsclean image, ignoring it");
					imageUrl = null;
				}
				if (string.IsNullOrEmpty(imageUrl))
				{
					Logger.Debug("No audio artwork image found from " + url);
					return null;
				}

				// Now make a request to actually get the image file
				byte[] responseData = client.DownloadData(imageUrl);
				Logger.Debug("Requesting audio artwork image from " + imageUrl);
				using (MemoryStream stream = new MemoryStream(responseData))
				{
					Bitmap b = new Bitmap(stream);
					Logger.Debug("Read " + responseData.Length + " for " + imageUrl);
					return b;
				}
			}
		}
	}
}