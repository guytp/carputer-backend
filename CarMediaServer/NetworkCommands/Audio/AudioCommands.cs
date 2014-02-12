namespace CarMediaServer
{
    /// <summary>
    /// Defines the different commands in the audio category.
    /// </summary>
    public enum AudioCommands : ushort
    {
        /// <summary>
        /// Indicates the command to retrieve the current media library of all connected devices.
        /// </summary>
        LibraryGet = 0x00,

		/// <summary>
		/// Indicates the command to add one or more items to the playlist queue, or to replace
		/// the playlist queue.
		/// </summary>
		PlaylistQueue = 0x01,

		/// <summary>
		/// Indicates a command to jump to a specified location in the playlist.
		/// </summary>
		PlaylistJump = 0x02,

		/// <summary>
		/// Indicates a command to toggle between play/pause.
		/// </summary>
		PauseToggle = 0x03,

		/// <summary>
		/// Indicates a command to jump to a specified place within a track.
		/// </summary>
		TrackJump = 0x04,

		/// <summary>
		/// Indicates a command to move to the next track in the playlist.
		/// </summary>
		PlaylistNext = 0x05,

		/// <summary>
		/// Indicates a command to move to the previous track in the playlist.
		/// </summary>
		PlaylistPrevious = 0x06,

		/// <summary>
		/// Indicates a command to toggle the shuffle state.
		/// </summary>
		ToggleShuffle = 0x07,
		
		/// <summary>
		/// Indicates a command to toggle the repeat all status.
		/// </summary>
		ToggleRepeatAll = 0x08,

		/// <summary>
		/// Indicates a command to get one or more items of artwork.
		/// </summary>
		ArtworkGet = 0x09
    }
}