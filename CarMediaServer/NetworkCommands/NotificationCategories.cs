namespace CarMediaServer
{
    /// <summary>
    /// Defines the different notification categories that can be sent.
    /// </summary>
    public enum NotificationCategories : ushort
    {
        /// <summary>
        /// Indicates a notification about dialers.
        /// </summary>
        Dialer = 0x00,

		/// <summary>
		/// Indicates a notification about media.
		/// </summary>
		Media = 0x01
    }
}