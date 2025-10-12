using SquidCraft.Core.Extensions.Container;
using SquidCraft.Core.Extensions.Services;
using SquidCraft.Network.Data.Services;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Interfaces.Processors;
using SquidCraft.Network.Interfaces.Services;
using SquidCraft.Network.Processors;
using SquidCraft.Network.Services;
using DryIoc;

namespace SquidCraft.Network.Extensions;

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
    public static IContainer RegisterNetworkMessage<T>(this IContainer container) where T : ISquidCraftMessage, new()
    {
        container.AddToRegisterTypedList(new NetworkMessageData(typeof(T), new T().MessageType));
        return container;
    }


}
