using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CarMediaServer
{
    /// <summary>
    /// This abstract class provides base functionalities to allow descended processors to inherit from.
    /// </summary>
    public abstract class CommandProcessor
    {
        /// <summary>
        /// Gets the list of commands that this processor can handle.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<ushort, Dictionary<ushort, Type>> GetProcessors()
        {
            Dictionary<ushort, Dictionary<ushort, Type>> commands = new Dictionary<ushort, Dictionary<ushort, Type>>();
            ushort category = (ushort)CommandCategories.Diagnostics;
            commands[category] = new Dictionary<ushort, Type>();
            commands[category].Add((ushort)DiagnosticCommands.Echo, typeof(EchoCommandProcessor));

			category = (ushort)CommandCategories.Audio;
            commands[category] = new Dictionary<ushort, Type>();
            commands[category].Add((ushort)AudioCommands.LibraryGet, typeof(AudioLibraryGetCommandProcessor));
            commands[category].Add((ushort)AudioCommands.PlaylistQueue, typeof(PlaylistQueueCommandProcessor));
            commands[category].Add((ushort)AudioCommands.PauseToggle, typeof(PauseToggleCommandProcessor));
            commands[category].Add((ushort)AudioCommands.PlaylistJump, typeof(PlaylistJumpCommandProcessor));
            commands[category].Add((ushort)AudioCommands.TrackJump, typeof(TrackJumpCommandProcessor));
            commands[category].Add((ushort)AudioCommands.PlaylistNext, typeof(PlaylistNextCommandProcessor));
            commands[category].Add((ushort)AudioCommands.PlaylistPrevious, typeof(PlaylistPreviousCommandProcessor));
            commands[category].Add((ushort)AudioCommands.ToggleRepeatAll, typeof(ToggleRepeatCommandProcessor));
            commands[category].Add((ushort)AudioCommands.ToggleShuffle, typeof(ToggleShuffleCommandProcessor));
            commands[category].Add((ushort)AudioCommands.ArtworkGet, typeof(ArtworkGetCommandProcessor));


			/*
            category = (ushort)CommandCategories.Dialers;
            commands[category] = new Dictionary<ushort, Type>();
            commands[category].Add((ushort)DialerCommands.Get, typeof(Dialers.DialerGetCommandProcessor));
            commands[category].Add((ushort)DialerCommands.SmsGet, typeof(Dialers.SmsGetCommandProcessor));
            commands[category].Add((ushort)DialerCommands.SmsDelete, typeof(Dialers.SmsDeleteCommandProcessor));
            */

            return commands;
        }

        /// <summary>
        /// Process the command.
        /// </summary>
        /// <param name="command">
        /// The command to process.
        /// </param>
        /// <returns>
        /// The data to write to the client in response or null if no response is required.
        /// </returns>
        public abstract byte[] ProcessCommand(Command command);

        /// <summary>
        /// Converts the specified object to serialised JSON and then returns it as a UTF-8 encoded byte array.
        /// </summary>
        /// <param name="obj">
        /// The object to encode as a UTF-8 JSON byte array.
        /// </param>
        /// <returns>
        /// A UTF-8 encoded JSON byte array.
        /// </returns>
        protected byte[] ToJsonBuffer(object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
    }
}