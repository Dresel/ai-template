using Projects;

namespace FocusTemplate.Admin.Web.E2E;

// Its own AppHost with the TLS-offloading ingress in front of the BFF, which the rest of the suite runs without.
public sealed class IngressAppFixture : IAsyncLifetime
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
				["Features:TlsOffloadingIngress=true", "Features:Analytics=false", "Features:Mobile=false",],
				cancellationTokenSource.Token);

		this.app = await builder.BuildAsync(cancellationTokenSource.Token);
		await this.app.StartAsync(cancellationTokenSource.Token);

		// The ingress waits for the BFF, so its health covers both.
		await this.app.ResourceNotifications.WaitForResourceHealthyAsync("admin-bff-ingress", cancellationTokenSource.Token);

		BaseUrl = this.app.GetEndpoint("admin-bff-ingress", "https").ToString();
	}
}