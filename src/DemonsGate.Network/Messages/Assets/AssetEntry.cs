using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Assets;


[MemoryPackable]
public partial record AssetEntry(string FileName, AssetType AssetType);

