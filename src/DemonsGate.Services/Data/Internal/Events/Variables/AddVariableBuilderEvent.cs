namespace Spectra.Engine.Services.Data.Internal.Events.Variables;

public record AddVariableBuilderEvent(string VariableName, Func<object> Builder);
