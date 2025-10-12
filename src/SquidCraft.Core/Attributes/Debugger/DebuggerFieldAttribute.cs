namespace SquidCraft.Core.Attributes.Debugger;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
/// <summary>
/// Marks a field or property for debugger display.
/// </summary>
public class DebuggerFieldAttribute : Attribute
{
}
