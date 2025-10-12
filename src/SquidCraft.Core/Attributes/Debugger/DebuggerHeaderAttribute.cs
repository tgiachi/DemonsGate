namespace SquidCraft.Core.Attributes.Debugger;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
/// <summary>
/// Specifies a header for debugger display.
/// </summary>
public class DebuggerHeaderAttribute : Attribute
{
    public DebuggerHeaderAttribute(string header) => Header = header;
    public string Header { get; }
}
