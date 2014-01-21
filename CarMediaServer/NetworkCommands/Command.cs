using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CarMediaServer
{
    /// <summary>
    /// A command is an abstract base class that represents any activity that can be conducted on the server-side.
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// Defines a dictionary that ties each of the command categories (key) with another dictionary that itself ties the opcodes within that category (key) to the type of command.
        /// </summary>
        private static Dictionary<ushort, Dictionary<ushort, Type>> _commandTypes = new Dictionary<ushort, Dictionary<ushort, Type>>();

        static Command()
        {
            // Create commands that are known for default types
            _commandTypes.Add((ushort)CommandCategories.Diagnostics, DiagnosticCommand.Commands);
            //_commandTypes.Add((ushort)CommandCategories.Dialers, Dialers.DialerCommand.Commands);
            _commandTypes.Add((ushort)CommandCategories.Audio, AudioCommand.Commands);
        }

        /// <summary>
        /// Create a command from supplied parameters.  If the type of command requested is not known or if the opcode within the command category cannot be found then a null valiue
        /// is returned.
        /// </summary>
        /// <param name="commandCategory">
        /// The command category to create a command from.
        /// </param>
        /// <param name="opCode">
        /// The op code of the command to create.
        /// </param>
        /// <param name="data">
        /// The data of the command to create if applicable otherwise null.
        /// </param>
        /// <returns>
        /// A Command object or null for an unknown command type.
        /// </returns>
        public static Command CreateCommand(ushort commandCategory, ushort opCode, string data)
        {
            // If the specified command category / opcode combiantion cannot be found, return a null command.
            if (!_commandTypes.ContainsKey(commandCategory))
                return null;
            Dictionary<ushort, Type> commands = _commandTypes[commandCategory];
            if (!commands.ContainsKey(opCode))
                return null;

            // Instantiate an instance of the command
            return (Command)Activator.CreateInstance(commands[opCode], data);
        }
    }
}