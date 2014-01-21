namespace CarMediaServer
{
    /// <summary>
    /// Defines the different command categories that exist.
    /// </summary>
    public enum CommandCategories : ushort
    {
        /// <summary>
        /// Indicates commands to maipulate dialers.
        /// </summary>
        Dialers = 0x00,

		/// <summary>
		/// Indicates commands to manipulate audio files.
		/// </summary>
		Audio = 0x01,

        /// <summary>
        /// Indicates diagnostic commands.
        /// </summary>
        Diagnostics = 0xFF
    }
}