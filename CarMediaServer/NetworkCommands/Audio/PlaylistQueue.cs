using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CarMediaServer
{
    /// <summary>
    /// Represents a command that instructs the system to playback one or more audio
	/// files via the MP3 subsystem. 
    /// </summary>
    public class PlaylistQueue : AudioCommand
    {
		#region Properties
		/// <summary>
		/// Gets the request that this command has received..
		/// </summary>
		public PlaylistQueueRequest Request { get; private set; }
		#endregion

		#region Constructors
        /// <summary>
        /// Creates a new PlaylistQueue command.
        /// </summary>
        /// <param name="json">
        /// The raw JSON that is passed to this command.
        /// </param>
        public PlaylistQueue(string json)
        {
			// There is a bug in mono deserializing int[] in Json so this is a horrible
			// workaround for now (Mono bug 12322)
            //Request = JsonConvert.DeserializeObject<PlaylistQueueRequest>(json);
			if (string.IsNullOrEmpty(json))
				throw new Exception("Missing JSON");
			if (!json.Contains("\"ReplaceCurrentQueue\""))
				throw new Exception("Error finding ReplaceCurrentQueue");
			string[] toks = json.Split(new string[] { "\"ReplaceCurrentQueue\"" }, StringSplitOptions.None);
			if (toks.Length != 2)
				throw new Exception("Invalid toks count at ReplaceCurrentQueue");
			toks = toks[1].Split(new char[] { ' ', ':', ',', '}', '"' }, StringSplitOptions.RemoveEmptyEntries);
			bool replaceCurrentQueue;
			if (!bool.TryParse(toks[0], out replaceCurrentQueue))
				throw new Exception("Error parsing replaceCurrentQueue - got " + json);
			toks = json.Split(new string[] { "\"AudioFileIds\"" }, StringSplitOptions.None);
			if (toks.Length != 2)
				throw new Exception("Invalid toks count at AudioFileIds");
			toks = toks[1].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
			List<int> audioFileIds = new List<int>();
			if (toks.Length == 3)
			{
				toks = toks[1].Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string intValue in toks)
				{
					int i = 0;
					if (!int.TryParse (intValue, out i))
						throw new Exception("Error parsing audio file id: " + i);
					audioFileIds.Add(i);
				}
			}
			Request = new PlaylistQueueRequest { ReplaceCurrentQueue = replaceCurrentQueue, AudioFileIds = audioFileIds.ToArray() };
        }
		#endregion
    }
}