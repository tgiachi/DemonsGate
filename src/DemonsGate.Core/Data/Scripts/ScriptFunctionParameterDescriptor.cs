namespace DemonsGate.Core.Data.Scripts;

/// <summary>
///     Record containing metadata about a script function parameter for documentation generation
/// </summary>
/// <param name="ParameterName">The name of the parameter</param>
/// <param name="ParameterType">The TypeScript type of the parameter</param>
/// <param name="RawParameterType">The raw .NET type of the parameter</param>
/// <param name="ParameterTypeString">String representation of the parameter type</param>
public record ScriptFunctionParameterDescriptor(
    string ParameterName,
    string ParameterType,
    Type RawParameterType,
    string ParameterTypeString
);
