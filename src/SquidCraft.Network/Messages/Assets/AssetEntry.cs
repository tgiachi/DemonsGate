using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Assets;


[MemoryPackable]
public partial record AssetEntry(string FileName, AssetType AssetType);

