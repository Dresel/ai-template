#pragma warning disable CA1711
namespace Argus.Worker.IntegrationTests;

/// <summary>
/// Serializes the test classes that drive the shared Anvil chain. A listener starting with a
/// fresh cursor begins at the tip, so blocks mined concurrently by another class can push a
/// just-deployed contract behind the starting point and out of the walk.
/// </summary>
[CollectionDefinition(Name)]
public sealed class AnvilCollection
{
	public const string Name = "anvil";
}