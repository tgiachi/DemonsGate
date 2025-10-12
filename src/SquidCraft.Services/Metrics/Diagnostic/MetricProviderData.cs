namespace SquidCraft.Services.Metrics.Diagnostic;

/// <summary>
/// public class MetricProviderData.
/// </summary>
public class MetricProviderData
{
    public string Name { get; set; }
    public object Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public MetricProviderData(string name, object value)
    {
        Name = name;
        Value = value;
    }

    public MetricProviderData()
    {

    }
}
