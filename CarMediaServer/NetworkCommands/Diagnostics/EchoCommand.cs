using Newtonsoft.Json;

namespace CarMediaServer
{
    /// <summary>
    /// Represents an echo command which is used to test that the server is responding in a timely fashion.
    /// </summary>
    public class EchoCommand : DiagnosticCommand
    {
        /// <summary>
        /// Gets or sets the request for this command.
        /// </summary>
        public EchoRequest Request { get; private set; }

        /// <summary>
        /// Creates a new echo command.
        /// </summary>
        /// <param name="json">
        /// The raw JSON that is passed to this command.
        /// </param>
        public EchoCommand(string json)
        {
            Request = JsonConvert.DeserializeObject<EchoRequest>(json);
        }
    }
}