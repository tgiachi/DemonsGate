using System.Text.Json.Serialization;
using SquidCraft.Game.Data.Assets;
using SquidCraft.Game.Data.Types;

namespace SquidCraft.Game.Data.Context;

[JsonSerializable(typeof(BlockDefinitionData))]
[JsonSerializable(typeof(BlockDefinitionData[]))]
[JsonSerializable(typeof(SideType))]
public partial class SquidCraftGameJsonContext : JsonSerializerContext
{
}
