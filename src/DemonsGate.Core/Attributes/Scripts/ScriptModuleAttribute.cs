namespace DemonsGate.Core.Attributes.Scripts;

/// <summary>
///     Attribute to mark a class as a script module that will be exposed to JavaScript
/// </summary>
/// <param name="name">The name under which the module will be accessible in JavaScript</param>
[AttributeUsage(AttributeTargets.Class)]
public class ScriptModuleAttribute(string name, string? helpText = null) : Attribute
{
    public string Name { get; } = name;

    public string? HelpText { get; } = helpText;
}
