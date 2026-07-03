namespace Pimly.AspNetCore.Observability;

/// <summary>Observability yapılandırma seçenekleri.</summary>
public sealed class ObservabilityOptions
{
    /// <summary>Configuration section adı.</summary>
    public const string SectionName = "Observability";

    /// <summary>Gets or sets a value indicating whether observability özelliklerinin etkin olup olmadığı.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets OpenTelemetry servis adı.</summary>
    public string ServiceName { get; set; } = "pimly-api";

    /// <summary>Gets or sets OpenTelemetry servis sürümü.</summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>Gets or sets Prometheus metrik endpoint yolu.</summary>
    public string MetricsPath { get; set; } = "/metrics";

    /// <summary>Gets or sets istek loglamasından hariç tutulacak path'ler.</summary>
    public string[] ExcludePathsFromRequestLogging { get; set; } =
    [
        "/healthz",
        "/ready",
        "/metrics",
        "/media",
    ];

    /// <summary>Gets or sets distributed tracing seçenekleri.</summary>
    public TracingOptions Tracing { get; set; } = new();
}

/// <summary>Distributed tracing yapılandırması.</summary>
public sealed class TracingOptions
{
    /// <summary>Gets or sets a value indicating whether tracing'in etkin olup olmadığı.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets OTLP gRPC exporter endpoint'i.</summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>Gets or sets head-based sampling oranı (0.0–1.0).</summary>
    public double SamplingRatio { get; set; } = 1.0;

    /// <summary>Gets or sets a value indicating whether EF span'lerine SQL ifadesi eklenip eklenmeyeceği.</summary>
    public bool IncludeSqlStatements { get; set; }
}
