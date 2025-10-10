using DemonsGate.Network.Args;
using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Interfaces.Services;
using DemonsGate.Network.Messages.Pings;
using DemonsGate.Services.Game.Data.Sessions;
using DemonsGate.Services.Game.Impl;
using DemonsGate.Services.Interfaces;
using NSubstitute;

namespace DemonsGate.Tests.Services.Game;

[TestFixture]
public class NetworkManagerServiceTests
{
    private INetworkService _networkService = null!;
    private IEventLoopService _eventLoopService = null!;
    private NetworkManagerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _networkService = Substitute.For<INetworkService>();
        _eventLoopService = Substitute.For<IEventLoopService>();

        _eventLoopService.EnqueueTask(Arg.Any<string>(), Arg.Any<Func<Task>>())
            .Returns(callInfo =>
            {
                var taskFunc = callInfo.Arg<Func<Task>>();
                taskFunc.Invoke().GetAwaiter().GetResult();
                return Guid.NewGuid().ToString();
            });

        _service = new NetworkManagerService(_networkService, _eventLoopService);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _service.StopAsync();
    }

    [Test]
    public void AddListener_WithNullListener_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _service.AddListener(null!));
    }

    [Test]
    public async Task MessageReceived_ShouldDispatchToRegisteredListeners()
    {
        PlayerNetworkSession? capturedSession = null;
        IDemonsGateMessage? capturedMessage = null;

        _service.AddListener(
            (session, message) =>
            {
                capturedSession = session;
                capturedMessage = message;
                return Task.CompletedTask;
            }
        );

        await _service.StartAsync();

        var message = new PingMessage();

        _networkService.ClientMessageReceived += Raise.Event<INetworkService.NetworkClientMessageHandler>(
            this,
            new NetworkClientMessageEventArgs(7, message, message.MessageType)
        );

        Assert.That(capturedSession, Is.Not.Null);
        Assert.That(capturedSession!.SessionId, Is.EqualTo(7));
        Assert.That(capturedMessage, Is.EqualTo(message));
    }

    [Test]
    public async Task StopAsync_ShouldUnsubscribeFromMessageEvents()
    {
        var dispatchCount = 0;

        _service.AddListener(
            (_, _) =>
            {
                dispatchCount++;
                return Task.CompletedTask;
            }
        );

        await _service.StartAsync();

        var message = new PingMessage();
        var args = new NetworkClientMessageEventArgs(5, message, message.MessageType);

        _networkService.ClientMessageReceived += Raise.Event<INetworkService.NetworkClientMessageHandler>(
            this,
            args
        );

        await _service.StopAsync();

        _networkService.ClientMessageReceived += Raise.Event<INetworkService.NetworkClientMessageHandler>(
            this,
            args
        );

        Assert.That(dispatchCount, Is.EqualTo(1));
    }
}
