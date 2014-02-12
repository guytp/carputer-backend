using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CarMediaServer
{
	/// <summary>
	/// This notification is fired frequently as new media becomes available on the device or as
	/// existing media devices such as memory sticks change their content during the next auto-discovery
	/// scan.
	/// </summary>
	public class AudioLibraryUpdateNotification : INetworkNotification
	{
		/// <summary>
		/// Gets or sets the files deleted from this source in this last updated.
		/// </summary>
		public List<int> DeletedFiles { get; set; }

		/// <summary>
		/// Gets or sets the new files that have been added to this device in this discovery scan.
		/// </summary>
		public List<AudioFile> NewFiles { get; set; }

		/// <summary>
		/// Gets or sets the files on this device that have now come online.
		/// </summary>
		public List<AudioFile> OnlineFiles { get; set; }

		/// <summary>
		/// Gets or sets the files on this device that have gone offline when a device is unplugged.
		/// </summary>
		public List<int> OfflineFiles { get; set; }

		/// <summary>
		/// Gets or sets any audio files that have been updated on the device - such as their MP3 tag
		/// information having changed.
		/// </summary>
		public List<AudioFile> UpdatedFiles { get; set; }

		/// <summary>
		/// Gets whether or not there are enough change deltas in this notification that it is worth sending
		/// over the network.
		/// </summary>
		[JsonIgnore]
		public bool ReadyToSend { get { return OfflineFiles.Count + DeletedFiles.Count + NewFiles.Count + OnlineFiles.Count + UpdatedFiles.Count > 100; } }
		
        /// <summary>
        /// Gets the category of this notification.
        /// </summary>
		public ushort NotificationCategory { get { return (ushort)0x01; } }

        /// <summary>
        /// Gets the code for this notification.
        /// </summary>
		public ushort NotificationCode { get { return (ushort)0x01; } }

		/// <summary>
		/// Create a new instance of this class.
		/// </summary>
		public AudioLibraryUpdateNotification()
		{
			DeletedFiles = new List<int>();
			NewFiles = new List<AudioFile>();
			OnlineFiles = new List<AudioFile>();
			OfflineFiles = new List<int>();
			UpdatedFiles = new List<AudioFile>();
		}

		/// <summary>
		/// Serialises the notification.
		/// </summary>
		/// <returns>
		/// The notification serialized as a byte array.
		/// </returns>
		public byte[] SerialiseNotification()
		{
			// Due to a Json.Net bug serialising int[] we construct this message manually
			string deletedFilesString = string.Empty;
			if ((DeletedFiles != null) && (DeletedFiles.Count > 0))
			{
				foreach (int audioFileId in DeletedFiles)
					deletedFilesString += (!string.IsNullOrEmpty(deletedFilesString) ? ", " : string.Empty) + audioFileId;
				deletedFilesString = "[" + deletedFilesString + "]";
			}
			else
				deletedFilesString = "null";
			string offlineFilesString = string.Empty;
			if ((OfflineFiles != null) && (OfflineFiles.Count > 0))
			{
				foreach (int audioFileId in OfflineFiles)
					offlineFilesString += (!string.IsNullOrEmpty(offlineFilesString) ? ", " : string.Empty) + audioFileId;
				offlineFilesString = "[" + offlineFilesString + "]";
			}
			else
				offlineFilesString = "null";
			string newFilesString = JsonConvert.SerializeObject(NewFiles);
			string updatedFilesString = JsonConvert.SerializeObject(UpdatedFiles);
			string onlineFilesString = JsonConvert.SerializeObject(OnlineFiles);
			string json = "{\"DeletedFiles\":" + deletedFilesString + ", \"OfflineFiles\":" + offlineFilesString + ", \"OnlineFiles\":" + onlineFilesString + ", \"NewFiles\":" + newFilesString + ", \"UpdatedFiles\":" + updatedFilesString + "}";
			return System.Text.Encoding.UTF8.GetBytes(json);
		}
	}
}