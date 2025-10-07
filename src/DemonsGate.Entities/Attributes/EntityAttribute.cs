namespace DemonsGate.Entities.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class EntityAttribute : Attribute
{
    public string DefaultFileName { get; set; }

    public EntityAttribute(string defaultFileName)
    {
        DefaultFileName = defaultFileName;
    }
}
