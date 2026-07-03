#pragma warning disable IDE0046

namespace FocusTemplate.AppHost;

internal static class AnalyticsExtensions
{
	public static IResourceBuilder<ProjectResource> WithUmamiAnalytics(
		this IResourceBuilder<ProjectResource> app,
		IResourceBuilder<UmamiResource> umami,
		string websiteName,
		string websiteDomain)
	{
		app.WithReference(umami)
			.WithEnvironment("Client__Analytics__Provider", "umami")
			.WithEnvironment("Client__Analytics__ScriptUrl", "_analytics/script.js");

		string? configuredWebsiteId = app.ApplicationBuilder.Configuration["Parameters:umami-website-id"];
		if (!string.IsNullOrEmpty(configuredWebsiteId))
		{
			return app.WithEnvironment("Client__Analytics__WebsiteId", configuredWebsiteId);
		}

		if (app.ApplicationBuilder.ExecutionContext.IsPublishMode)
		{
			return app;
		}

		return app.WaitFor(umami)
			.WithEnvironment(async context =>
			{
				string? websiteId = await UmamiSeeder.EnsureWebsiteAsync(
					umami.GetEndpoint("http").Url,
					websiteName,
					websiteDomain,
					context.CancellationToken);

				if (websiteId is not null)
				{
					context.EnvironmentVariables["Client__Analytics__WebsiteId"] = websiteId;
				}
			});
	}
}