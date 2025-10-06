using DemonsGate.Core.Data.Internal;
using DemonsGate.Core.Extensions.Container;
using DryIoc;

namespace DemonsGate.Core.Extensions.Services;

/// <summary>
///     Extension methods for registering services in the dependency injection container
/// </summary>
public static class ServiceRegistrationExtension
{
    /// <summary>
    ///     Registers a service with a specific implementation type in the container
    /// </summary>
    /// <param name="container">The dependency injection container</param>
    /// <param name="serviceType">The service interface type</param>
    /// <param name="implementationType">The implementation type</param>
    /// <param name="priority">Priority for service ordering (default 0)</param>
    /// <returns>The container instance for method chaining</returns>
    public static IContainer AddService(
        this IContainer container, Type serviceType, Type implementationType, int priority = 0
    )
    {
        ArgumentNullException.ThrowIfNull(container);

        ArgumentNullException.ThrowIfNull(serviceType);

        ArgumentNullException.ThrowIfNull(implementationType);

        container.Register(serviceType, implementationType, Reuse.Singleton);

        container.AddToRegisterTypedList(new ServiceDefinitionObject(serviceType, implementationType, priority));

        return container;
    }

    /// <summary>
    ///     Registers a service where the implementation type is the same as the service type
    /// </summary>
    /// <param name="container">The dependency injection container</param>
    /// <param name="serviceType">The service type (used for both interface and implementation)</param>
    /// <param name="priority">Priority for service ordering (default 0)</param>
    /// <returns>The container instance for method chaining</returns>
    public static IContainer AddService(this IContainer container, Type serviceType, int priority = 0)
    {
        return AddService(container, serviceType, serviceType, priority);
    }
}
