namespace CarMediaServer
{
    /// <summary>
    /// The request object for an PlaylistQueue allowing parameters to be serialised.
    /// </summary>
    public class PlaylistQueueRequest
    {
        /// <summary>
        /// Gets or sets the files to add or replace in the queue.
        /// </summary>
		public int[] AudioFileIds { get; set; }

		/// <summary>
		/// Gets or sets whether the existing queue should be replaced first.
		/// </summary>
		public bool ReplaceCurrentQueue { get; set; }
    }
}