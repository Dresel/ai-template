using Testcontainers.Redis;

namespace SpatialFocus.Testing.Api;

// One Redis for the run. A test class's hosts share a channel prefix of their own, so no class hears another's signals
public sealed class RedisFixture : IAsyncLifetime
{
	private readonly RedisContainer container = new RedisBuilder("redis:8.6").Build();

	public string ConnectionStringFor(string channelPrefix) => $"{this.container.GetConnectionString()},channelPrefix={channelPrefix}";

	public async ValueTask DisposeAsync() => await this.container.DisposeAsync();

	public async ValueTask InitializeAsync() => await this.container.StartAsync(TestContext.Current.CancellationToken);
}