namespace SquidCraft.Core.Attributes.Scripts;

/// <summary>
///     Attribute to mark a method as a script function that will be exposed to JavaScript.
/// </summary>
/// <param name="functionName">Optional name override for the script function.</param>
/// <param name="helpText">Optional help text describing the function's purpose.</param>
[AttributeUsage(AttributeTargets.Method)]
public class ScriptFunctionAttribute(string? functionName = null, string? helpText = null) : Attribute
{
    public string? FunctionName { get; } = functionName;
    public string? HelpText { get; } = helpText;
}
