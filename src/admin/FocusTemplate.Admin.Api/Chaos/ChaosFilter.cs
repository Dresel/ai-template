using System.Globalization;
using Polly;
using Polly.Simmy;

namespace FocusTemplate.Admin.Api.Chaos;

internal sealed class ChaosFilter(ResiliencePipeline<object?> pipeline) : IEndpointFilter
{
	public static ChaosFilter FromConfiguration(IConfigurationSection section) =>
		new(
			new ResiliencePipelineBuilder<object?>()
				.AddChaosLatency(
					Rate(section, "LatencyRate"),
					TimeSpan.Parse(section["Latency"] ?? "0", CultureInfo.InvariantCulture))
				.AddChaosOutcome(
					Rate(section, "FaultRate"),
					static () => Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
				.Build());

	// An injected outcome replaces the endpoint's answer, which then never runs
	public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
		pipeline.ExecuteAsync(_ => next(context), context.HttpContext.RequestAborted);

	private static double Rate(IConfigurationSection section, string key) =>
		double.Parse(section[key] ?? "0", CultureInfo.InvariantCulture);
}