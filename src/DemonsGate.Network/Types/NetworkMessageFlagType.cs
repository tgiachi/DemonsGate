namespace DemonsGate.Network.Types;


[Flags]
public enum NetworkMessageFlagType
{
    None = 0,
    Compressed,
    Encrypted
}
