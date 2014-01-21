using Newtonsoft.Json;

namespace CarMediaServer
{
    /// <summary>
    /// Represents a command that does not need to parse any data.
    /// </summary>
    public class EmptyCommand : DiagnosticCommand
    {
        /// <summary>
        /// Creates a new EmptyCommand command.
        /// </summary>
        /// <param name="json">
        /// The raw JSON that is passed to this command.
        /// </param>
        public EmptyCommand(string json)
        {
        }
    }
}