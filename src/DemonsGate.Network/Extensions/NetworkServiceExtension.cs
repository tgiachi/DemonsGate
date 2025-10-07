using DemonsGate.Core.Extensions.Container;
using DemonsGate.Core.Extensions.Services;
using DemonsGate.Network.Data.Services;
using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Interfaces.Processors;
using DemonsGate.Network.Interfaces.Services;
using DemonsGate.Network.Processors;
using DemonsGate.Network.Services;
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

        container.AddService<INetworkService, DefaultNetworkService>();

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


}
