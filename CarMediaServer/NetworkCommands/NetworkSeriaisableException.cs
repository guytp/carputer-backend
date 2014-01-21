using System;

namespace CarMediaServer
{
    /// <summary>
    /// A basic implementation of INetworkSerialisableException that simply contains a NetworkErrorMessage for manipulation.
    /// </summary>
    public class NetworkSerialisableException : Exception
    {
        /// <summary>
        /// Gets the error message to be serialised across the network for this exception.
        /// </summary>
        public NetworkErrorMessage NetworkErrorMessage { get; private set; }

        public NetworkSerialisableException(string code, string message, string details, Exception innerException)
            : base(message, innerException)
        {
            NetworkErrorMessage = new NetworkErrorMessage
            {
                ErrorCode = code,
                Message = message,
                Details = string.IsNullOrEmpty(details) ? innerException == null ? null : innerException.ToString() : details
            };
        }
    }
}