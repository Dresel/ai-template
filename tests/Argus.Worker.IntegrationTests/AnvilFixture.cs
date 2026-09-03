using System.Text.RegularExpressions;
using Argus.Worker.IntegrationTests;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

[assembly: AssemblyFixture(typeof(AnvilFixture))]

namespace Argus.Worker.IntegrationTests;

/// <summary>
/// Runs an Anvil (Foundry) node in a container - a local EVM chain with prefunded accounts.
/// </summary>
public sealed class AnvilFixture : IAsyncLifetime
{
	/// <summary>
	/// Anvil's default chain id.
	/// </summary>
	public const long ChainId = 31337;

	/// <summary>
	/// Well-known private key of Anvil's prefunded account 0.
	/// </summary>
	public const string DeployerPrivateKey = "0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80";

	private const int RpcPort = 8545;

	private readonly IContainer container = new ContainerBuilder("ghcr.io/foundry-rs/foundry:stable")
		.WithEntrypoint("anvil")
		.WithCommand("--host", "0.0.0.0")
		.WithPortBinding(RpcPort, true)
		.WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(new Regex("Listening on", RegexOptions.None, TimeSpan.FromSeconds(1))))
		.Build();

	public string RpcUrl => $"http://{this.container.Hostname}:{this.container.GetMappedPublicPort(RpcPort)}";

	public async ValueTask DisposeAsync() => await this.container.DisposeAsync();

	public async ValueTask InitializeAsync() => await this.container.StartAsync(TestContext.Current.CancellationToken);
}