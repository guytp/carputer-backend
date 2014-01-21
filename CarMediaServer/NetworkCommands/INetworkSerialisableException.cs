using System;

namespace CarMediaServer
{
    /// <summary>
    /// The INetworkSerialisableException indicates that a particular exception exposes a NetworkErrorMessage
    /// implementation of itself.
    /// </summary>
    public interface INetworkSerialisableException
    {
        NetworkErrorMessage NetworkErrorMessage { get; }
    }
}