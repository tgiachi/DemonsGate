using DemonsGate.Core.Enums;

namespace DemonsGate.Network.Data;

public class NetworkConfig
{
    public CompressionType CompressionType { get; set; } = CompressionType.None;
    public EncryptionType EncryptionType { get; set; } = EncryptionType.None;
    public string EncryptionKey { get; set; } = string.Empty;

}
