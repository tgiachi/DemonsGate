namespace Spectra.Engine.Services.Data.Internal.Events.Scheduler;

/// <summary>
/// public record AddSchedulerJobEvent(string Name, TimeSpan TotalSpan, Func<Task> Action);.
/// </summary>
public record AddSchedulerJobEvent(string Name, TimeSpan TotalSpan, Func<Task> Action);
