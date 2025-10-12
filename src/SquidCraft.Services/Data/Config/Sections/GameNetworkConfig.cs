using SquidCraft.Core.Enums;

namespace SquidCraft.Services.Data.Config.Sections;

/// <summary>
/// Represents the network configuration for the game.
/// </summary>
public class GameNetworkConfig
{
    public int Port { get; set; } = 7666;
    public CompressionType CompressionType { get; set; } = CompressionType.None;
    public EncryptionType EncryptionType { get; set; } = EncryptionType.None;
    public string EncryptionKey { get; set; } = string.Empty;

}
