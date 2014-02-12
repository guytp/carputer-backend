using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CarMediaServer
{
	/// <summary>
	/// This notification is fired whenever a new piece of audio artwork is availble.
	/// </summary>
	public class AudioArtworkAvailableNotification : INetworkNotification
	{
		public string Artist { get; set; }

		public string Album { get; set; }

		public byte[] ImageContent { get; set; }

        /// <summary>
        /// Gets the category of this notification.
        /// </summary>
		public ushort NotificationCategory { get { return (ushort)0x01; } }

        /// <summary>
        /// Gets the code for this notification.
        /// </summary>
		public ushort NotificationCode { get { return (ushort)0x02; } }

		/// <summary>
		/// Create a new instance of this class.
		/// </summary>
		public AudioArtworkAvailableNotification()
		{
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
			string base64Image = ImageContent == null || ImageContent.Length < 1 ? "null" : "\"" + Convert.ToBase64String(ImageContent) + "\"";
			string json = "{\"Artist\": " + (string.IsNullOrEmpty(Artist) ? "null" : "\"" + Artist + "\"") + ", \"Album\": " + (string.IsNullOrEmpty(Album) ? "null" : "\"" + Album + "\"") + ", \"ImageContent\": " + base64Image + "}";

			return System.Text.Encoding.UTF8.GetBytes(json);
		}
	}
}