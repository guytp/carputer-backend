using System;
using System.Collections.Generic;

namespace CarMediaServer
{
    /// <summary>
    /// Represents an abstract audio command which is used to manage audio content.
    /// </summary>
    public abstract class AudioCommand : Command
    {
        /// <summary>
        /// Defines a list of all known audio commands.
        /// </summary>
        internal static Dictionary<ushort, Type> Commands { get; private set; }
        
        /// <summary>
        /// Static initialiser to setup the class.
        /// </summary>
        static AudioCommand()
        {
            Commands = new Dictionary<ushort, Type>();
            Commands.Add((ushort)AudioCommands.LibraryGet, typeof(EmptyCommand));
            Commands.Add((ushort)AudioCommands.PlaylistQueue, typeof(PlaylistQueue));
            Commands.Add((ushort)AudioCommands.PauseToggle, typeof(EmptyCommand));
            Commands.Add((ushort)AudioCommands.PlaylistJump, typeof(PlaylistJump));
            Commands.Add((ushort)AudioCommands.TrackJump, typeof(TrackJump));
            Commands.Add((ushort)AudioCommands.PlaylistPrevious, typeof(EmptyCommand));
            Commands.Add((ushort)AudioCommands.PlaylistNext, typeof(EmptyCommand));
            Commands.Add((ushort)AudioCommands.ToggleRepeatAll, typeof(EmptyCommand));
            Commands.Add((ushort)AudioCommands.ToggleShuffle, typeof(EmptyCommand));
            Commands.Add((ushort)AudioCommands.ArtworkGet, typeof(ArtworkGet));
        }
    }
}