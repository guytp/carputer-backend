using System;
using System.Threading;
using System.Web;
using System.Drawing;

namespace CarMediaServer
{
	/// <summary>
	/// This class is responsible for finding audio artwork, downloading it to the device and
	/// making it available for clients.
	/// </summary>
	public class AudioArtworkDiscoverer
	{
		public AudioArtworkDiscoverer()
		{
			// Setup last.fm client		

			// Start the thread
			new Thread(DiscoveryThread) { Name = "Audio Artwork Discovery", IsBackground = true }.Start();
		}

		private void DiscoveryThread()
		{
			LastFmClient client = new LastFmClient();
			while (true)
			{
				try
				{
					// Find artist/album details that we require
					AudioFile[] audioFiles = AudioFileFactory.ApplicationInstance.FilesRequiringArtworkScan();
					if (audioFiles == null)
					{
						Logger.Debug("No files requiring artwork checks, waiting 30 seconds");
						Thread.Sleep(30000);
						continue;
					}

					foreach (AudioFile audioFile in audioFiles)
					{
						// Determine what (if any) artwork we have for these items
						AudioArtwork artistArtwork = string.IsNullOrEmpty(audioFile.Artist) ? null : AudioArtworkFactory.ApplicationInstance.GetForArtist(audioFile.Artist);
						AudioArtwork albumArtwork = string.IsNullOrEmpty(audioFile.Artist) || string.IsNullOrEmpty(audioFile.Album) ? null : AudioArtworkFactory.ApplicationInstance.GetForAlbum(audioFile.Artist, audioFile.Album);

						// Get artist artwork if required
						if ((artistArtwork == null) && (!string.IsNullOrEmpty(audioFile.Artist)))
						{
							try
							{
								// Make a request for the artist artwork and create appropriate
								// artistArtwork object
								Logger.Debug("Searching for artist artwork for " + audioFile.Artist);
								using (Bitmap artistImage = client.GetArtistImage(audioFile.Artist))
								{
									AudioArtwork artwork = new AudioArtwork { Artist = audioFile.Artist };
									AudioArtworkFactory.ApplicationInstance.Create(artwork);
									bool hasImage = false;
									if (artistImage == null)
									{
										Logger.Info("No artist image for " + audioFile.Artist + " available");
									}
									else
									{
										Logger.Info("Found artist image for " + audioFile.Artist);
										artwork.Save(artistImage);
										hasImage = true;
									}

									// Now trigger a notification indicating this artwork is available
									Controller.NotificationNetworkServer.SendNotification(new AudioArtworkAvailableNotification
                                  	{
										Artist = audioFile.Artist,
										ImageContent = hasImage ? artwork.Load() : null
									});
								}
							}
							catch (Exception ex)
							{
								Logger.Info("Failed to get album artwork for " + audioFile.Artist + ": " + ex.Message);
							}
						}

						// Get album artwork if required
						if ((albumArtwork == null) && (!string.IsNullOrEmpty(audioFile.Artist)) && (!string.IsNullOrEmpty(audioFile.Album)))
						{
							try
							{
								// Make a request for the artist artwork and create appropriate
								// artistArtwork object
								Logger.Debug("Searching for artist artwork for " + audioFile.Artist + " - " + audioFile.Album);
								using (Bitmap albumImage = client.GetAlbumImage(audioFile.Artist, audioFile.Album))
								{
									AudioArtwork artwork = new AudioArtwork { Artist = audioFile.Artist, Album = audioFile.Album };
									AudioArtworkFactory.ApplicationInstance.Create(artwork);
									bool hasImage = false;
									if (albumImage == null)
									{
										Logger.Info("No album image for " + audioFile.Artist + " - " + audioFile.Album + " available");
									}
									else
									{
										Logger.Info("Found artist image for " + audioFile.Artist + " - " + audioFile.Album);
										artwork.Save(albumImage);
										hasImage = true;
									}

									// Now trigger a notification indicating this artwork is available
									Controller.NotificationNetworkServer.SendNotification(new AudioArtworkAvailableNotification
                                  	{
										Artist = audioFile.Artist,
										Album = audioFile.Album,
										ImageContent = hasImage ? artwork.Load() : null
									});
								}
							}
							catch (Exception ex)
							{
								Logger.Info("Failed to get album artwork for " + audioFile.Artist + " - " + audioFile.Album + ": " + ex.Message);
							}
						}

						// Update this audio file as checked
						audioFile.ArtworkSearchDate = DateTime.Now;
						AudioFileFactory.ApplicationInstance.Update(audioFile);

						Thread.Sleep(1000);
					}
					Thread.Sleep(5000);
				}
				catch (Exception ex)
				{
					Logger.Error("Error in artwork discovery loop", ex);
				}

				Thread.Sleep(500);
			}
		}
	}
}

