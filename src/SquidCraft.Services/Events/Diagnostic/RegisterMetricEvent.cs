using SquidCraft.Core.Interfaces.Metrics;

namespace SquidCraft.Services.Events.Diagnostic;

/// <summary>
/// public record RegisterMetricEvent(IMetricsProvider provider);.
/// </summary>
public record RegisterMetricEvent(IMetricsProvider provider);
