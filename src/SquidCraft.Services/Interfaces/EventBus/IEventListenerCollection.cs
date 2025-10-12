namespace SquidCraft.Services.Interfaces.EventBus;

/// <summary>
///     Interface for type-safe listener collections
/// </summary>
internal interface IEventListenerCollection
{
    int Count { get; }
}
