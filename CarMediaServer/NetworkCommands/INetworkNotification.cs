using Newtonsoft.Json;

namespace CarMediaServer
{
    public interface INetworkNotification
    {
        /// <summary>
        /// Gets the category of this notification.
        /// </summary>
        [JsonIgnore]
        ushort NotificationCategory { get; }

        /// <summary>
        /// Gets the code for this notification.
        /// </summary>
        [JsonIgnore]
        ushort NotificationCode { get; }

		/// <summary>
		/// Serialises the notification.
		/// </summary>
		/// <returns>
		/// The notification serialized as a byte array.
		/// </returns>
		byte[] SerialiseNotification();
    }
}