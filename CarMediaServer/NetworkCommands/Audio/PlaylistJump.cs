using System;

namespace CarMediaServer
{
	/// <summary>
	/// This command jumps the playlist to the specified location.
	/// </summary>
	public class PlaylistJump : AudioCommand
	{
		#region Properties
		/// <summary>
		/// Gets the location in the playlist that should be jumped to.
		/// </summary>
		public int Position { get; private set; }
		#endregion

		#region Constructors
        /// <summary>
        /// Creates a new PlaylistQueue command.
        /// </summary>
        /// <param name="json">
        /// The raw JSON that is passed to this command.
        /// </param>
        public PlaylistJump(string json)
        {
			Position = int.Parse(json);
		}
		#endregion
	}
}