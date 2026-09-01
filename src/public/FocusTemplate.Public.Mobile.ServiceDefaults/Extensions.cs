using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

// Adds common Aspire client services to a .NET MAUI app: service discovery, resilience, and OpenTelemetry.
// Counterpart of FocusTemplate.ServiceDefaults for mobile clients (no ASP.NET Core dependency).
// Generated from the `maui-aspire-servicedefaults` template, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
	public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
		where TBuilder : IHostApplicationBuilder
	{
		builder.ConfigureOpenTelemetry();

		builder.Services.AddServiceDiscovery();

		builder.Services.ConfigureHttpClientDefaults(http =>
		{
			// Turn on resilience by default
			http.AddStandardResilienceHandler();

			// Turn on service discovery by default
			http.AddServiceDiscovery();
		});

		// WASM/MAUI apps never start IHostedService instances, so OpenTelemetry providers must be
		// initialized eagerly through MAUI's initialization hook.
		builder.Services.TryAddEnumerable(
			ServiceDescriptor.Transient<IMauiInitializeService, OpenTelemetryInitializer>(_ =>
				new OpenTelemetryInitializer()));

		return builder;
	}

	public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
		where TBuilder : IHostApplicationBuilder
	{
		builder.Logging.AddOpenTelemetry(logging =>
		{
			logging.IncludeFormattedMessage = true;
			logging.IncludeScopes = true;
		});

		builder.Services.AddOpenTelemetry()
			.WithMetrics(metrics =>
			{
				// Add "Microsoft.Maui" to also report metrics from the .NET MAUI SDK (can be noisy).
				metrics.AddHttpClientInstrumentation().AddRuntimeInstrumentation();
			})
			.WithTracing(tracing =>
			{
				// Add "Microsoft.Maui" as a source to also report traces from the .NET MAUI SDK (can be noisy).
				tracing.AddSource(builder.Environment.ApplicationName).AddHttpClientInstrumentation();
			});

		builder.AddOpenTelemetryExporters();

		return builder;
	}

	private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
		where TBuilder : IHostApplicationBuilder
	{
		bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

		if (useOtlpExporter)
		{
			builder.Services.AddOpenTelemetry().UseOtlpExporter();
		}

		return builder;
	}

	private sealed class OpenTelemetryInitializer : IMauiInitializeService
	{
		public void Initialize(IServiceProvider services)
		{
			services.GetService<MeterProvider>();
			services.GetService<TracerProvider>();
			services.GetService<LoggerProvider>();
		}
	}
}