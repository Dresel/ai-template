using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

public static class BlazorClientExtensions
{
	// An omitted serviceName falls back to the AppHost-injected OTEL_SERVICE_NAME, which ProxyBlazorTelemetry sets to "{host-resource} (client)".
	public static WebAssemblyHostBuilder AddBlazorClientServiceDefaults(
		this WebAssemblyHostBuilder builder,
		string? serviceName = null)
	{
		ComponentsMetricsServiceCollectionExtensions.AddComponentsMetrics(builder.Services);
		ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(builder.Services);

		builder.ConfigureBlazorClientOpenTelemetry(serviceName);

		return builder;
	}

	private static WebAssemblyHostBuilder ConfigureBlazorClientOpenTelemetry(
		this WebAssemblyHostBuilder builder,
		string? serviceName)
	{
		// Without an OTLP path base, there's nowhere to export telemetry in WASM.
		string? otlpPathBase = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
		if (string.IsNullOrEmpty(otlpPathBase))
		{
			return builder;
		}

		serviceName ??= builder.Configuration["OTEL_SERVICE_NAME"]!;

		// Resolve the OTLP path against the page's origin so telemetry goes through
		// the same origin the user navigated to, avoiding cross-origin issues.
		Uri baseAddress = new(builder.HostEnvironment.BaseAddress);
		Uri otlpEndpoint = new(baseAddress, $"{otlpPathBase}/");

		builder.Services.AddOpenTelemetry()

			// Use a fixed instanceId so all browser tabs report as a single service instance
			// in the dashboard rather than spawning separate entries per tab.
			.ConfigureResource(r => r.AddService(serviceName, serviceInstanceId: serviceName))
			.WithLogging(
				logging => logging.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint, "v1/logs")),
				options =>
				{
					options.IncludeFormattedMessage = true;
					options.IncludeScopes = true;
				})
			.WithMetrics(metrics =>
			{
				metrics.AddMeter("Microsoft.AspNetCore.Components");
				metrics.AddMeter("Microsoft.AspNetCore.Components.Lifecycle");
				metrics.AddHttpClientInstrumentation();
				metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint, "v1/metrics"));
			})
			.WithTracing(tracing =>
			{
				tracing.AddSource("Microsoft.AspNetCore.Components").AddHttpClientInstrumentation();
				tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint, "v1/traces"));
			});

		return builder;
	}
}