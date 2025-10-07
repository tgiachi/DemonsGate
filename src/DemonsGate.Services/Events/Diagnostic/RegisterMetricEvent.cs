using DemonsGate.Core.Interfaces.Metrics;

namespace DemonsGate.Services.Events.Diagnostic;

/// <summary>
/// public record RegisterMetricEvent(IMetricsProvider provider);.
/// </summary>
public record RegisterMetricEvent(IMetricsProvider provider);
