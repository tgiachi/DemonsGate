namespace DemonsGate.Core.Data.Internal;

/// <summary>
/// Represents a service definition with type information and priority.
/// </summary>
public record ServiceDefinitionObject(Type ServiceType, Type ImplementationType, int Priority = 0);

