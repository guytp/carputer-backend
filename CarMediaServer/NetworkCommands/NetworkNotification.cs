using Newtonsoft.Json;
using System;

namespace CarMediaServer
{
    /// <summary>
    /// This class provides support functions for network notifications.
    /// </summary>
    public static class NetworkNotification
    {
        /// <summary>
        /// Creates a new INetworkNotification object from supplied data.
        /// </summary>
        /// <param name="category">
        /// The category of notification to create.
        /// </param>
        /// <param name="code">
        /// The code of the notification to create.
        /// </param>
        /// <param name="json">
        /// The JSON payload defining the notification.
        /// </param>
        /// <returns>
        /// An INetworkNotification-based notification object or null if the category/code could not be looked up.
        /// </returns>
        public static INetworkNotification CreateNotification(ushort category, ushort code, string json)
        {
            // Attempt to parse the notification type based on category/code
            Type t = null;
			/*
            if (category == (ushort)NotificationCategories.Dialer)
            {
                if (code == (ushort)DialerNotifications.DialerDisconnected)
                    t = typeof(DialerDisconnectedNotification);
                else if (code == (ushort)DialerNotifications.DialerConnected)
                    t = typeof(DialerConnectedNotification);
                else if (code == (ushort)DialerNotifications.DialerInitialised)
                    t = typeof(DialerInitialisedNotification);
                else if (code == (ushort)DialerNotifications.NetworkChange)
                    t = typeof(NetworkChangeNotification);
                else if (code == (ushort)DialerNotifications.NewSms)
                    t = typeof(NewSmsNotification);
                else if (code == (ushort)DialerNotifications.SignalStrengthChange)
                    t = typeof(SignalStrengthChangeNotification);
            }
            */

            // Return if no type found
            if (t == null)
                return null;

            // Convert JSON to be the object type
            return (INetworkNotification)JsonConvert.DeserializeObject(json, t);
        }
    }
}