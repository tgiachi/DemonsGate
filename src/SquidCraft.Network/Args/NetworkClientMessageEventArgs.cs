using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Args;

/// <summary>
/// Represents event arguments for message reception.
/// </summary>
public record NetworkClientMessageEventArgs( int ClientId, ISquidCraftMessage Message, NetworkMessageType MessageType);

