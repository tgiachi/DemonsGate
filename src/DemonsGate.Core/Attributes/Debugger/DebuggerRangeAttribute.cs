namespace DemonsGate.Core.Attributes.Debugger;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
/// <summary>
/// public class DebuggerRangeAttribute : Attribute.
/// </summary>
public class DebuggerRangeAttribute : Attribute
{
    public DebuggerRangeAttribute(double min, double max, double step = 1)
    {
        Min = min;
        Max = max;
        Step = step;
    }

    public double Min { get; }
    public double Max { get; }
    public double Step { get; }
}
