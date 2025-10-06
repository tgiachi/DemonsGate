using DemonsGate.Core.Extensions.Container;
using DemonsGate.Network.Data.Config;
using DemonsGate.Network.Data.Services;
using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Interfaces.Processors;
using DemonsGate.Network.Processors;
using DryIoc;

namespace DemonsGate.Network.Extensions;

/// <summary>
///     Provides extension methods for registering network services in the DI container
/// </summary>
public static class NetworkServiceExtension
{
    /// <summary>
    ///     Registers network services (packet serializer and deserializer) in the container
    /// </summary>
    /// <param name="container">The DI container</param>
    /// <returns>The container for method chaining</returns>
    public static IContainer RegisterNetworkServices(this IContainer container)
    {
        container.Register<IPacketSerializer, DefaultPacketProcessor>(Reuse.Singleton);
        container.Register<IPacketDeserializer, DefaultPacketProcessor>(Reuse.Singleton);

        return container;
    }

    /// <summary>
    ///     Registers a network message type in the container
    /// </summary>
    /// <typeparam name="T">The message type to register</typeparam>
    /// <param name="container">The DI container</param>
    /// <returns>The container for method chaining</returns>
    public static IContainer RegisterNetworkMessage<T>(this IContainer container) where T : IDemonsGateMessage, new()
    {
        container.AddToRegisterTypedList(new NetworkMessageData(typeof(T), new T().MessageType));
        return container;
    }

    /// <summary>
    ///     Registers a network configuration instance in the container
    /// </summary>
    /// <param name="container">The DI container</param>
    /// <param name="config">The network configuration to register</param>
    /// <returns>The container for method chaining</returns>
    public static IContainer WithNetworkConfig(this IContainer container, NetworkConfig config)
    {
        container.RegisterInstance(config);
        return container;
    }
}
