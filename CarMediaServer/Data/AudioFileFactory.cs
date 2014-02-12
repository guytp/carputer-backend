using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace CarMediaServer
{
    /// <summary>
    /// This class provides access to the database for AudioFile objects.
    /// </summary>
    public class AudioFileFactory : Factory<AudioFile, Int32>
    {
        /// <summary>
        /// Gets a handle to a one-off instantiated instance of the AudioFileFactory.  This should be used for non-thread
        /// safe related database calls where there is no need for creating your own factory.
        /// </summary>
        public static AudioFileFactory ApplicationInstance { get; private set; }

        /// <summary>
        /// One off static construction of this factory class.
        /// </summary>
        static AudioFileFactory()
        {
            ApplicationInstance = new AudioFileFactory();
        }

		/// <summary>
		/// Removes all audio files for a particular UUID that are older than a specified time.
		/// </summary>
		/// <param name='uuid'>
		/// The UUID to remove for.
		/// </param>
		/// <param name='lastAcceptableDate'>
		/// The last acceptable date.
		/// </param>
		public void RemoveForUuid(string uuid, DateTime lastAcceptableDate)
		{
            MySqlConnection conn = null;
            try
            {
                // Open the connection
                Logger.Debug("AudioFileFactory RemoveForUuid " + uuid + " older than " + lastAcceptableDate);
                conn = new MySqlConnection(Configuration.ConnectionString);
                conn.Open();

                // Creation the insertion SQL and corresponding properties
                MySqlCommand command = new MySqlCommand();
                command.CommandText = "DELETE FROM AudioFile WHERE DeviceUuid=@uuid AND LastSeen<@lastAcceptableDate";
                command.Parameters.AddWithValue("@uuid", uuid);
                command.Parameters.AddWithValue("@lastAcceptableDate", lastAcceptableDate);
                command.CommandType = System.Data.CommandType.Text;
                command.Connection = conn;

                // Execute the command
                Logger.Debug("AudioFileFactory RemoveForUuid executing command");
                command.ExecuteNonQuery();

                // Update cache
                Logger.Debug("AudioFileFactory RemoveForUuid invalidating cache");
                InvalidateCache();
                Logger.Debug("AudioFileFactory RemoveForUuid cache invalidated");
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }
		}

		public void MergeChanges(List<AudioFile> newFiles, List<AudioFile> onlineFiles, List<AudioFile> updatedFiles)
		{
			try
			{
				using (MySqlConnection conn = new MySqlConnection(Configuration.ConnectionString))
				{
					conn.Open();

					// Add all new files in a batch
					if ((newFiles != null) && (newFiles.Count > 0))
					{
						string temporaryFile = Path.GetTempFileName();
						using (FileStream file = File.Open(temporaryFile, FileMode.Open))
						{
							foreach (AudioFile audioFile in newFiles)
							{
								try
								{
									byte[] bytes = System.Text.Encoding.UTF8.GetBytes(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\n",
									                         MySqlEscape(audioFile.Artist),
									                         MySqlEscape(audioFile.Title),
									                         audioFile.TrackNumber.HasValue ? audioFile.TrackNumber.Value.ToString() : "\\N",
									                         MySqlEscape(audioFile.Album),
									                         audioFile.DurationSeconds.HasValue ? audioFile.DurationSeconds.Value.ToString() : "\\N",
									                         audioFile.DeviceUuid,
									                         MySqlEscape(audioFile.RelativePath), audioFile.LastSeen.ToString("yyyy-MM-dd HH:mm:ss"))
									);
									file.Write(bytes, 0, bytes.Length);
								}
								catch (Exception ex)
								{
									Logger.Error("Error processing a new file in MergeChanges()", ex);
								}
							}
							file.Close();
						}
						ProcessStartInfo startInfo = new ProcessStartInfo
						{
							Arguments = "666 \"" + temporaryFile + "\"",
							UseShellExecute = false,
							FileName = "/bin/chmod"
						};
						Process p = Process.Start(startInfo);
						p.WaitForExit();
						using (MySqlCommand comm = conn.CreateCommand())
						{
							comm.CommandText = "LOAD DATA INFILE '" + temporaryFile + "' INTO TABLE AudioFile (Artist, Title, TrackNumber, Album, Duration, DeviceUuid, RelativePath, LastSeen)";
							comm.CommandType = System.Data.CommandType.Text;
							comm.ExecuteNonQuery();
						}
						File.Delete(temporaryFile);

						// Update the cache for these new files
						_cache.CacheObject(newFiles.ToArray(), false);
					}

					// For all of those simply online just update their last updated time
					if (onlineFiles != null && onlineFiles.Count > 0)
					{
						StringBuilder onlineFilesClause = new StringBuilder();
						foreach (AudioFile audioFile in onlineFiles)
						{
							if (onlineFilesClause.Length > 0)
								onlineFilesClause.Append(", ");
							onlineFilesClause.Append(audioFile.AudioFileId);
						}
						using (MySqlCommand comm = conn.CreateCommand())
						{
							comm.CommandText = "UPDATE AudioFile SET LastSeen='" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE AudioFileId IN (" + onlineFilesClause + ")";
							comm.CommandType = System.Data.CommandType.Text;
							comm.ExecuteNonQuery();
						}
					}

					// For updated files we need to run an update for each one
					if (updatedFiles != null)
						foreach (AudioFile audioFile in updatedFiles)
							Update(audioFile, false);

					// Delete any no longer relevant artwork
					using (MySqlCommand comm = conn.CreateCommand())
					{
						comm.CommandText = "DELETE FROM AudioArtwork WHERE Artist NOT IN (SELECT DISTINCT Artist FROM AudioFile)";
						comm.CommandType = System.Data.CommandType.Text;
						comm.ExecuteNonQuery();
					}
					AudioArtworkFactory.ApplicationInstance._cache.InvalidateCache();
				}
			}
			catch(Exception ex)
			{
				Logger.Fatal("Fatal error updating audio database", ex);
			}
		}

		public AudioFile[] FilesRequiringArtworkScan()
		{
			AudioFile[] all = ReadAll();
			return all.Where(f => !f.ArtworkSearchDate.HasValue).ToArray();
		}

		private static string MySqlEscape(string value)
		{
			if (value == null)
				return "\\N";
			return value.Replace("\\", "\\\\").Replace("\t", "\\\t").Replace("\n", "\\\n");
		}
    }
}