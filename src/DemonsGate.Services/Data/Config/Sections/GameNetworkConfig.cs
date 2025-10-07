using DemonsGate.Core.Enums;

namespace DemonsGate.Services.Data.Config.Sections;

public class GameNetworkConfig
{
    public int Port { get; set; } = 7666;
    public CompressionType CompressionType { get; set; } = CompressionType.None;
    public EncryptionType EncryptionType { get; set; } = EncryptionType.None;
    public string EncryptionKey { get; set; } = string.Empty;

}
