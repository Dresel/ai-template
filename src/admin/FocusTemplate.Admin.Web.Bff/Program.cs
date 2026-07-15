using FocusTemplate.Admin.Web.Bff;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<ClientConfiguration>(builder.Configuration.GetSection("Client"));

// Filter out OTLP proxy traffic from tracing to prevent a feedback loop: YARP forwards /_otlp/*
// requests to the dashboard, and without filtering, those forwarding requests would themselves be
// traced and exported — creating recursive telemetry entries in the dashboard.
builder.Services.PostConfigure<AspNetCoreTraceInstrumentationOptions>(options =>
{
	Func<HttpContext, bool>? previous = options.Filter;
	options.Filter = context =>
	{
		string? path = context.Request.Path.Value;
		return (previous is null || previous(context)) && (path is null || !path.Contains("/_otlp/", StringComparison.Ordinal));
	};
});

builder.Services.PostConfigure<HttpClientTraceInstrumentationOptions>(options =>
{
	Func<HttpRequestMessage, bool>? previous = options.FilterHttpRequestMessage;
	options.FilterHttpRequestMessage = request =>
		(previous is null || previous(request)) &&
		(request.RequestUri is null || !request.RequestUri.AbsolutePath.StartsWith("/v1/", StringComparison.Ordinal));
});

// YARP for proxying service calls from the WASM client
builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
	.AddServiceDiscoveryDestinationResolver();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// Serve the Blazor WASM client configuration endpoint.
// The Aspire host sets Client__ConfigEndpointPath and Client__ConfigResponse
// as environment variables; the server reads them and serves the JSON response.
string? configEndpointPath = app.Configuration["Client:ConfigEndpointPath"];
string? configResponse = app.Configuration["Client:ConfigResponse"];

if (!string.IsNullOrEmpty(configEndpointPath) && !string.IsNullOrEmpty(configResponse))
{
	app.MapGet(configEndpointPath, () => Results.Content(configResponse, "application/json"));
}

app.MapGet("/client-configuration", (IOptions<ClientConfiguration> config) => Results.Json(config.Value));

if (app.Environment.IsDevelopment())
{
	app.MapDebugEndpoint();
}

app.MapStaticAssets();
app.MapReverseProxy();

app.MapFallbackToFile("index.html");

app.Run();