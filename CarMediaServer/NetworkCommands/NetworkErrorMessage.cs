using System;

namespace CarMediaServer
{
    /// <summary>
    /// This class represents an error that is passed between network servers simiar to a SOAP fault.
    /// </summary>
    public class NetworkErrorMessage
    {
        /// <summary>
        /// Indicates the error code used when creating 
        /// </summary>
        public static readonly string UnhandledExceptionErrorCode = "hubserver:unhandledexception";

        /// <summary>
        /// Gets or sets the unique code for the error.
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the human-readable error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the optional details of the error such as stack traces.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Create a new instance of a NetworkErrorMessage.
        /// </summary>
        public NetworkErrorMessage()
        {
        }

        /// <summary>
        /// Create a new instance of a NetworkErrorMessage.
        /// </summary>
        /// <param name="ex">
        /// The exception to pre-populate the network error message from.
        /// </param>
        public NetworkErrorMessage(Exception ex)
        {
            ErrorCode = UnhandledExceptionErrorCode;
            Message = "Unhandled exception of type " + ex.GetType().Name;
            Details = ex.ToString();
        }
    }
}