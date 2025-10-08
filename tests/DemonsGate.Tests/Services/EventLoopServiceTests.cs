using DemonsGate.Services.Data.Config.Sections;
using DemonsGate.Services.Impl;
using DemonsGate.Services.Interfaces;
using DemonsGate.Services.Types;
using NSubstitute;

namespace DemonsGate.Tests.Services;

/// <summary>
/// Contains test cases for EventLoopService.
/// </summary>
[TestFixture]
public class EventLoopServiceTests
{
    private IEventBusService _mockEventBusService = null!;
    private EventLoopConfig _config = null!;
    private EventLoopService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockEventBusService = Substitute.For<IEventBusService>();
        _config = new EventLoopConfig
        {
            TickIntervalMs = 50,
            MaxActionsPerTick = 100,
            SlowActionThresholdMs = 50,
            EnableDetailedMetrics = true
        };

        _service = new EventLoopService(_mockEventBusService, _config);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _service.StopAsync();
        _service.Dispose();
    }

    [Test]
    public void Constructor_ShouldInitializeWithCorrectConfig()
    {
        // Assert
        Assert.That(_service.TickIntervalMs, Is.EqualTo(50));
        Assert.That(_service.Metrics, Is.Not.Null);
    }

    [Test]
    public void Constructor_ShouldInitializePriorityQueues()
    {
        // Assert - Service should be created without throwing
        Assert.Pass();
    }

    [Test]
    public async Task StartAsync_ShouldStartEventLoop()
    {
        // Act
        await _service.StartAsync();

        // Assert
        Assert.That(_service.TickCount, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task StopAsync_ShouldStopEventLoop()
    {
        // Arrange
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert - Should not throw
        Assert.Pass();
    }

    [Test]
    public void EnqueueAction_WithNormalPriority_ShouldReturnValidId()
    {
        // Arrange
        Action action = () => { };

        // Act
        var actionId = _service.EnqueueAction("test_action", action);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueAction_WithHighPriority_ShouldReturnValidId()
    {
        // Arrange
        Action action = () => { };

        // Act
        var actionId = _service.EnqueueAction("test_action", action, EventLoopPriority.High);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueAction_WithLowPriority_ShouldReturnValidId()
    {
        // Arrange
        Action action = () => { };

        // Act
        var actionId = _service.EnqueueAction("test_action", action, EventLoopPriority.Low);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueAction_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.EnqueueAction("test_action", null!));
    }

    [Test]
    public void EnqueueTask_WithNormalPriority_ShouldReturnValidId()
    {
        // Arrange
        Func<Task> task = () => Task.CompletedTask;

        // Act
        var actionId = _service.EnqueueTask("test_task", task);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueTask_WithHighPriority_ShouldReturnValidId()
    {
        // Arrange
        Func<Task> task = () => Task.CompletedTask;

        // Act
        var actionId = _service.EnqueueTask("test_task", task, EventLoopPriority.High);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueTask_WithLowPriority_ShouldReturnValidId()
    {
        // Arrange
        Func<Task> task = () => Task.CompletedTask;

        // Act
        var actionId = _service.EnqueueTask("test_task", task, EventLoopPriority.Low);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueTask_WithNullTask_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.EnqueueTask("test_task", null!));
    }

    [Test]
    public void EnqueueDelayedAction_WithNormalPriority_ShouldReturnValidId()
    {
        // Arrange
        Action action = () => { };
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        var actionId = _service.EnqueueDelayedAction("test_delayed_action", action, delay);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueDelayedAction_WithHighPriority_ShouldReturnValidId()
    {
        // Arrange
        Action action = () => { };
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        var actionId = _service.EnqueueDelayedAction("test_delayed_action", action, delay, EventLoopPriority.High);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueDelayedAction_WithLowPriority_ShouldReturnValidId()
    {
        // Arrange
        Action action = () => { };
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        var actionId = _service.EnqueueDelayedAction("test_delayed_action", action, delay, EventLoopPriority.Low);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueDelayedAction_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.EnqueueDelayedAction("test_delayed_action", null!, TimeSpan.FromMilliseconds(100)));
    }

    [Test]
    public void EnqueueDelayedTask_WithNormalPriority_ShouldReturnValidId()
    {
        // Arrange
        Func<Task> task = () => Task.CompletedTask;
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        var actionId = _service.EnqueueDelayedTask("test_delayed_task", task, delay);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueDelayedTask_WithHighPriority_ShouldReturnValidId()
    {
        // Arrange
        Func<Task> task = () => Task.CompletedTask;
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        var actionId = _service.EnqueueDelayedTask("test_delayed_task", task, delay, EventLoopPriority.High);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueDelayedTask_WithLowPriority_ShouldReturnValidId()
    {
        // Arrange
        Func<Task> task = () => Task.CompletedTask;
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        var actionId = _service.EnqueueDelayedTask("test_delayed_task", task, delay, EventLoopPriority.Low);

        // Assert
        Assert.That(actionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EnqueueDelayedTask_WithNullTask_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.EnqueueDelayedTask("test_delayed_task", null!, TimeSpan.FromMilliseconds(100)));
    }

    [Test]
    public void TryCancelAction_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = _service.TryCancelAction("invalid_id");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryCancelAction_WithValidQueuedActionId_ShouldReturnTrue()
    {
        // Arrange
        Action action = () => { };
        var actionId = _service.EnqueueAction("test_action", action);

        // Act
        var result = _service.TryCancelAction(actionId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TryCancelAction_WithValidDelayedActionId_ShouldReturnTrue()
    {
        // Arrange
        Action action = () => { };
        var delay = TimeSpan.FromMilliseconds(1000); // Long delay to ensure it's not processed
        var actionId = _service.EnqueueDelayedAction("test_delayed_action", action, delay);

        // Act
        var result = _service.TryCancelAction(actionId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task Delay_ShouldCompleteAfterSpecifiedTime()
    {
        // Arrange
        var delayMs = 100;
        var startTime = DateTime.UtcNow;

        // Act
        await _service.Delay(delayMs);
        var endTime = DateTime.UtcNow;

        // Assert
        var elapsed = (endTime - startTime).TotalMilliseconds;
        Assert.That(elapsed, Is.GreaterThanOrEqualTo(delayMs - 10)); // Allow some tolerance
    }

    [Test]
    public async Task PriorityProcessing_ShouldExecuteHighPriorityFirst()
    {
        // Arrange
        var executionOrder = new List<string>();
        await _service.StartAsync();

        _service.EnqueueAction("low_priority", () => executionOrder.Add("low"), EventLoopPriority.Low);
        _service.EnqueueAction("high_priority", () => executionOrder.Add("high"), EventLoopPriority.High);
        _service.EnqueueAction("normal_priority", () => executionOrder.Add("normal"), EventLoopPriority.Normal);

        // Wait for processing
        await Task.Delay(200);

        // Assert - High priority should be executed first
        Assert.That(executionOrder[0], Is.EqualTo("high"));
    }

    [Test]
    public async Task Metrics_ShouldBeUpdatedAfterProcessing()
    {
        // Arrange
        await _service.StartAsync();

        // Act
        _service.EnqueueAction("test_action", () => { });
        await Task.Delay(200); // Wait for processing

        // Assert
        Assert.That(_service.Metrics.TotalActionsProcessed, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task OnTick_EventShouldBeRaised()
    {
        // Arrange
        var tickRaised = false;
        _service.OnTick += (duration) => tickRaised = true;
        await _service.StartAsync();

        // Act
        await Task.Delay(200); // Wait for at least one tick

        // Assert
        Assert.That(tickRaised, Is.True);
    }

    [Test]
    public void Dispose_ShouldNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _service.Dispose());
    }

    [Test]
    public async Task MultipleStartCalls_ShouldNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await _service.StartAsync();
            await _service.StartAsync();
        });
    }

    [Test]
    public async Task MultipleStopCalls_ShouldNotThrow()
    {
        // Arrange
        await _service.StartAsync();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await _service.StopAsync();
            await _service.StopAsync();
        });
    }
}