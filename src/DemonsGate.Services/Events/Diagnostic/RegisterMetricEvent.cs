using DemonsGate.Core.Interfaces.Metrics;

namespace DemonsGate.Services.Events.Diagnostic;

public record RegisterMetricEvent(IMetricsProvider provider);
