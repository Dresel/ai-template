namespace SpatialFocus.Testing.Api;

// One database per test class (xunit builds one fixture per class) and a Respawn reset before every test, so test
// classes share nothing and run in parallel without a collection. Arrange exactly the rows the test asserts.
public abstract class ApiTestBase<TFixture>(TFixture factory) : IClassFixture<TFixture>, IAsyncLifetime
	where TFixture : class, IApiFixture
{
	protected TFixture Factory { get; } = factory;

	public virtual async ValueTask InitializeAsync() => await Factory.ResetAsync();

	public virtual ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}