using Projects;

namespace FocusTemplate.Web.E2E;

public sealed class BlazorAppFixture : IAsyncLifetime
{
	private DistributedApplication? app;

	public string BaseUrl { get; private set; } = string.Empty;

	public async ValueTask DisposeAsync()
	{
		if (this.app is not null)
		{
			await this.app.DisposeAsync();
		}
	}

	public async ValueTask InitializeAsync()
	{
		using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(120));

		IDistributedApplicationTestingBuilder appHost =
			await DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(cancellationTokenSource.Token);

		this.app = await appHost.BuildAsync(cancellationTokenSource.Token);
		await this.app.StartAsync(cancellationTokenSource.Token);
		await this.app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellationTokenSource.Token);

		BaseUrl = this.app.GetEndpoint("web", "http").ToString();
	}
}