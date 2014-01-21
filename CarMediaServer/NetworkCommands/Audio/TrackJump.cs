using System;

namespace CarMediaServer
{
	/// <summary>
	/// This command jumps the currently playing song to the specified location.
	/// </summary>
	public class TrackJump : AudioCommand
	{
		#region Properties
		/// <summary>
		/// Gets the location in the song (in seconds) that should be jumped to.
		/// </summary>
		public int Offset { get; private set; }
		#endregion

		#region Constructors
        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="json">
        /// The raw JSON that is passed to this command.
        /// </param>
        public TrackJump(string json)
        {
			Offset = int.Parse(json);
		}
		#endregion
	}
}