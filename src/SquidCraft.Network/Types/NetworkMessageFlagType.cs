namespace SquidCraft.Network.Types;

/// <summary>
///     Defines flags that can be applied to network messages
/// </summary>
[Flags]
public enum NetworkMessageFlagType
{
    /// <summary>No flags applied</summary>
    None = 0,

    /// <summary>Message payload is compressed</summary>
    Compressed,

    /// <summary>Message payload is encrypted</summary>
    Encrypted
}
