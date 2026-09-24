using FocusTemplate.Admin.Web.Bff;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<ClientConfiguration>(builder.Configuration.GetSection("Client"));

builder.AddBffAuthentication();

builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, BffJsonContext.Default));

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

builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
	.AddServiceDiscoveryDestinationResolver()
	.AddAccessTokenTransform();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

// The AppHost sets Client__ConfigEndpointPath and Client__ConfigResponse; the BFF serves that JSON to the WASM client.
string? configEndpointPath = app.Configuration["Client:ConfigEndpointPath"];
string? configResponse = app.Configuration["Client:ConfigResponse"];

if (!string.IsNullOrEmpty(configEndpointPath) && !string.IsNullOrEmpty(configResponse))
{
	app.MapGet(configEndpointPath, () => Results.Content(configResponse, "application/json"));
}

app.MapGet("/client-configuration", (IOptions<ClientConfiguration> config) => config.Value);

app.MapAuthenticationEndpoints();

if (app.Environment.IsDevelopment())
{
	app.MapDebugEndpoint();
}

app.MapStaticAssets();
app.MapReverseProxy();

app.MapFallbackToFile("index.html");

app.Run();