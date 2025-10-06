namespace DemonsGate.Core.Attributes.Debugger;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class DebuggerHeaderAttribute : Attribute
{
    public DebuggerHeaderAttribute(string header) => Header = header;
    public string Header { get; }
}
