namespace DemonsGate.Core.Attributes.Debugger;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
/// <summary>
/// public class DebuggerHeaderAttribute : Attribute.
/// </summary>
public class DebuggerHeaderAttribute : Attribute
{
    public DebuggerHeaderAttribute(string header) => Header = header;
    public string Header { get; }
}
