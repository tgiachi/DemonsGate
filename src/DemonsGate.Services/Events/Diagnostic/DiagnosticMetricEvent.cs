using DemonsGate.Services.Metrics.Diagnostic;

namespace DemonsGate.Services.Events.Diagnostic;

public record DiagnosticMetricEvent(MetricProviderData Metrics);
