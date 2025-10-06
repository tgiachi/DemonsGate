namespace DemonsGate.Core.Data.Internal;

public record ServiceDefinitionObject(Type ServiceType, Type ImplementationType, int Priority = 0);

