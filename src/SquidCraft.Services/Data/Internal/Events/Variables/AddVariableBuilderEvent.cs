namespace Spectra.Engine.Services.Data.Internal.Events.Variables;

/// <summary>
/// public record AddVariableBuilderEvent(string VariableName, Func<object> Builder);.
/// </summary>
public record AddVariableBuilderEvent(string VariableName, Func<object> Builder);
