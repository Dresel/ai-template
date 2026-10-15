namespace SpatialFocus.Testing.E2E;

// Its own AppHost with the TLS-offloading ingress in front of the BFF, which the rest of the suite runs without. The
// product creates the builder with the ingress switched on and names the ingress resource.
public abstract class IngressAppFixture(string ingressResource) : IAsyncLifetime
{
	private DistributedApplication? app;

	public string BaseUrl { get; private set; } = string.Empty;

	public async ValueTask DisposeAsync()
	{
		if (this.app is not null)
		{
			await this.app.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}

	public async ValueTask InitializeAsync()
	{
		using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(180));

		IDistributedApplicationTestingBuilder builder = await CreateBuilderAsync(cancellationTokenSource.Token);

		this.app = await builder.BuildAsync(cancellationTokenSource.Token);
		await this.app.StartAsync(cancellationTokenSource.Token);

		// The ingress waits for the BFF, so its health covers both.
		await this.app.ResourceNotifications.WaitForResourceHealthyAsync(ingressResource, cancellationTokenSource.Token);

		BaseUrl = this.app.GetEndpoint(ingressResource, "https").ToString();
	}

	protected abstract Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync(CancellationToken cancellationToken);
}