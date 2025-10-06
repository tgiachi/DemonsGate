namespace DemonsGate.Core.Attributes.Scripts;

/// <summary>
///     Attribute to mark a method as a script function that will be exposed to JavaScript
/// </summary>
/// <param name="helpText">Optional help text describing the function's purpose</param>
[AttributeUsage(AttributeTargets.Method)]
public class ScriptFunctionAttribute(string? helpText = null) : Attribute
{
    public string? HelpText { get; } = helpText;
}
