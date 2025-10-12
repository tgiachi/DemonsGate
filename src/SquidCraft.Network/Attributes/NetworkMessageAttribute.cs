using SquidCraft.Network.Types;

namespace SquidCraft.Network.Attributes;

/// <summary>
/// Marks a class as a network message that should be automatically registered.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NetworkMessageAttribute : Attribute
{
    /// <summary>
    /// Gets the type of network message.
    /// </summary>
    public NetworkMessageType MessageType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkMessageAttribute"/> class.
    /// </summary>
    /// <param name="messageType">The type of network message</param>
    public NetworkMessageAttribute(NetworkMessageType messageType)
    {
        MessageType = messageType;
    }
}
