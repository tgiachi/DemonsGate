using System.Text.Json.Serialization;
using SquidCraft.Client.Data;

namespace SquidCraft.Client.Context;

[JsonSerializable(typeof(AtlasDefinition))]
public partial class SquidCraftClientJsonContext : JsonSerializerContext
{

}
