using System.Text.Json.Serialization;
using DemonsGate.Game.Data.Assets;
using DemonsGate.Game.Data.Types;

namespace DemonsGate.Game.Data.Context;

[JsonSerializable(typeof(BlockDefinitionData))]
[JsonSerializable(typeof(BlockSideType))]
public partial class DemonsGateGameJsonContext : JsonSerializerContext
{
}
