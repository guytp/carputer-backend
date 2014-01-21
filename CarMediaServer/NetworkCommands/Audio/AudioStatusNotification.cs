using System;

namespace CarMediaServer
{
	/// <summary>
	/// Audio status notification.
	/// </summary>
	public class AudioStatusNotification : INetworkNotification
	{
		#region Properties
        /// <summary>
        /// Gets the current position within the active playlist as a zero based count.
        /// </summary>
		public int PlaylistPosition { get; set; }

        /// <summary>
        /// Gets an array of AudioFileIds that are in the current playlist.
        /// </summary>
		public int[] Playlist { get; set; }

        /// <summary>
        /// Gets whether or not playback is currently paused.
        /// </summary>
		public bool IsPaused { get; set; }

        /// <summary>
        /// Gets whether or not we are playing/paused (true) or stopped (false).
        /// </summary>
		public bool IsPlaying { get; set; }

        /// <summary>
        /// Gets the position in seconds through the current track.
        /// </summary>
		public int Position { get; set; }

        /// <summary>
        /// Gets the duration in seconds of the current track.
        /// </summary>
		public int Duration { get; set; }

        /// <summary>
        /// Gets the category of this notification.
        /// </summary>
		public ushort NotificationCategory { get { return (ushort)0x01; } }

        /// <summary>
        /// Gets the code for this notification.
        /// </summary>
		public ushort NotificationCode { get { return (ushort)0x00; } }

		/// <summary>
		/// Gets or sets whether the audio device is in shuffle mode.
		/// </summary>
		public bool IsShuffle { get; set; }

		/// <summary>
		/// Gets or sets whether we are in repeat all mode.
		/// </summary>
		public bool IsRepeatAll { get; set; }

		/// <summary>
		/// Gets or sets whether we can call next track.
		/// </summary>
		public bool CanMoveNext { get; set; }

		/// <summary>
		/// Gets or sets whether we can call previous track.
		/// </summary>
		public bool CanMovePrevious { get; set; }
		#endregion

		/// <summary>
		/// Serialises the notification.
		/// </summary>
		/// <returns>
		/// The notification serialized as a byte array.
		/// </returns>
		public byte[] SerialiseNotification()
		{
			// Due to a Json.Net bug serialising int[] we construct this message manually
			string playlistString = string.Empty;
			if ((Playlist != null) && (Playlist.Length > 0))
				foreach (int audioFileId in Playlist)
					playlistString += (!string.IsNullOrEmpty(playlistString) ? ", " : string.Empty) + audioFileId;
			string json = "{\"PlaylistPosition\":" + PlaylistPosition + ", \"Playlist\":[" + playlistString + "], \"IsPaused\":" + IsPaused.ToString().ToLower () + ", \"IsPlaying\":" + IsPlaying.ToString ().ToLower() + ", \"Position\":" + Position + ", \"Duration\":" + Duration + ", \"IsRepeatAll\":" + IsRepeatAll.ToString().ToLower() + ", \"IsShuffle\":" + IsShuffle.ToString().ToLower()+ ", \"CanMoveNext\":" + CanMoveNext.ToString().ToLower()+ ", \"CanMovePrevious\":" + CanMovePrevious.ToString().ToLower() + "}";
			return System.Text.Encoding.UTF8.GetBytes(json);
		}
	}
}