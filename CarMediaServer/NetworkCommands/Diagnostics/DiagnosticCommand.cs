using System;
using System.Collections.Generic;

namespace CarMediaServer
{
    /// <summary>
    /// Represents an abstract diagnostic command which can be uesd to find out information about the server.
    /// </summary>
    public abstract class DiagnosticCommand : Command
    {
        /// <summary>
        /// Defines a list of all known diagnostic commands.
        /// </summary>
        internal static Dictionary<ushort, Type> Commands { get; private set; }
        
        /// <summary>
        /// Static initialiser to setup the class.
        /// </summary>
        static DiagnosticCommand()
        {
            Commands = new Dictionary<ushort, Type>();
            Commands.Add((ushort)DiagnosticCommands.Echo, typeof(EchoCommand));
        }
    }
}