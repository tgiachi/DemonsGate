using SquidCraft.Network.Args;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Interfaces.Services;
using SquidCraft.Network.Messages.Pings;
using SquidCraft.Services.Game.Data.Sessions;
using SquidCraft.Services.Game.Impl;
using SquidCraft.Services.Interfaces;
using NSubstitute;

namespace SquidCraft.Tests.Services.Game;

[TestFixture]
public class NetworkManagerServiceTests
{
    private INetworkService _networkService = null!;
    private IEventLoopService _eventLoopService = null!;
    private ITimerService _timerService = null!;
    private NetworkManagerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _networkService = Substitute.For<INetworkService>();
        _eventLoopService = Substitute.For<IEventLoopService>();
        _timerService = Substitute.For<ITimerService>();

        _service = new NetworkManagerService(_networkService, _eventLoopService, _timerService);
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
        ISquidCraftMessage? capturedMessage = null;

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
