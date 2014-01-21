namespace CarMediaServer
{
    /// <summary>
    /// The request object for an EchoCommand allowing parameters to be serialised.
    /// </summary>
    public class EchoRequest
    {
        /// <summary>
        /// Gets or sets the message to echo.
        /// </summary>
        public string Message { get; set; }
    }
}