using Projects;

namespace FocusTemplate.Admin.Web.E2E;

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
		using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(180));

		IDistributedApplicationTestingBuilder builder =
			await DistributedApplicationTestingBuilder.CreateAsync<FocusTemplate_AppHost>(
				["Features:TlsOffloadingIngress=false", "Features:Analytics=false", "Features:Mobile=false", "Features:Intel=false",],
				cancellationTokenSource.Token);

		this.app = await builder.BuildAsync(cancellationTokenSource.Token);
		await this.app.StartAsync(cancellationTokenSource.Token);
		await this.app.ResourceNotifications.WaitForResourceHealthyAsync("admin-bff", cancellationTokenSource.Token);

		BaseUrl = this.app.GetEndpoint("admin-bff", "http").ToString();
	}
}