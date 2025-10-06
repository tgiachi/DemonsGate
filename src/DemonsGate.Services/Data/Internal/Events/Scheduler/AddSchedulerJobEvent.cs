namespace Spectra.Engine.Services.Data.Internal.Events.Scheduler;

public record AddSchedulerJobEvent(string Name, TimeSpan TotalSpan, Func<Task> Action);
