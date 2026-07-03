using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Pimly.AspNetCore.Observability;

/// <summary>PLGT observability kayıt ve middleware uzantıları.</summary>
public static class ObservabilityExtensions
{
    /// <summary>Serilog, health checks, OpenTelemetry metrik ve trace servislerini kaydeder.</summary>
    public static WebApplicationBuilder AddPimlyObservability(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        builder.Services.Configure<ObservabilityOptions>(
            builder.Configuration.GetSection(ObservabilityOptions.SectionName));

        if (!options.Enabled)
        {
            return builder;
        }

        builder.Host.UseSerilog((context, _, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.WithProperty("service", options.ServiceName)
            .Enrich.WithProperty("environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(new CompactJsonFormatter()));

        var databaseConnection = builder.Configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");
        var identityConnection = builder.Configuration.GetConnectionString("Identity")
            ?? throw new InvalidOperationException("Connection string 'Identity' is not configured.");

        builder.Services.AddHealthChecks()
            .AddNpgSql(databaseConnection, name: "catalog-db", tags: ["ready", "db"])
            .AddNpgSql(identityConnection, name: "identity-db", tags: ["ready", "db"])
            .AddCheck<MediaStorageHealthCheck>("media-storage", tags: ["ready"]);

        var otelBuilder = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName, serviceVersion: options.ServiceVersion));

        otelBuilder.WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter());

        if (options.Tracing.Enabled)
        {
            otelBuilder.WithTracing(tracing => tracing
                .SetSampler(new TraceIdRatioBasedSampler(options.Tracing.SamplingRatio))
                .AddAspNetCoreInstrumentation(instrumentation =>
                {
                    instrumentation.RecordException = true;
                    instrumentation.Filter = context =>
                        !IsExcludedObservabilityPath(context.Request.Path, options);
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(instrumentation =>
                {
                    instrumentation.SetDbStatementForText = options.Tracing.IncludeSqlStatements;
                })
                .AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = new Uri(options.Tracing.OtlpEndpoint);
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                }));
        }

        return builder;
    }

    /// <summary>Request logging, health endpoints, Prometheus scrape ve trace header middleware'ini uygular.</summary>
    public static WebApplication UsePimlyObservability(this WebApplication app)
    {
        var options = app.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        if (!options.Enabled)
        {
            app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
            return app;
        }

        app.UseWhen(
            context => !IsExcludedObservabilityPath(context.Request.Path, options),
            builder => builder.UseSerilogRequestLogging(loggingOptions =>
            {
                loggingOptions.GetLevel = (httpContext, _, exception) =>
                {
                    if (exception is not null)
                    {
                        return LogEventLevel.Error;
                    }

                    return httpContext.Response.StatusCode switch
                    {
                        >= 500 => LogEventLevel.Error,
                        >= 400 => LogEventLevel.Warning,
                        _ => LogEventLevel.Information,
                    };
                };

                loggingOptions.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                    diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
                    diagnosticContext.Set("UserId", HttpContextObservability.GetUserId(httpContext.User) ?? "(anonymous)");

                    var activity = Activity.Current;
                    if (activity is not null)
                    {
                        diagnosticContext.Set("OtelTraceId", activity.TraceId.ToString());
                    }
                };
            }));

        app.Use(async (context, next) =>
        {
            await next();

            if (IsExcludedObservabilityPath(context.Request.Path, options))
            {
                return;
            }

            var traceId = HttpContextObservability.GetTraceId(context);
            HttpContextObservability.AppendTraceIdHeader(context, traceId);

            if (context.Response.StatusCode >= StatusCodes.Status400BadRequest
                && context.Items[HttpContextObservability.FailureLoggedItemKey] is not true)
            {
                LogUnhandledHttpFailure(context);
            }
        });

        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthCheckResponseWriters.WriteLivenessResponse,
        });

        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriters.WriteReadinessResponse,
        });

        app.MapPrometheusScrapingEndpoint(options.MetricsPath);

        return app;
    }

    private static void LogUnhandledHttpFailure(HttpContext context)
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(ApiFailureLogger.LoggerCategory);

        logger.LogWarning(
            "Request {Method} {Path} failed with {StatusCode}. UserId={UserId}",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            HttpContextObservability.GetUserId(context.User) ?? "(anonymous)");
    }

    internal static bool IsExcludedObservabilityPath(PathString path, ObservabilityOptions options)
    {
        var value = path.Value ?? string.Empty;

        foreach (var excluded in options.ExcludePathsFromRequestLogging)
        {
            if (value.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
