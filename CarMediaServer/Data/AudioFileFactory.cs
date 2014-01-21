using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

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
    }
}