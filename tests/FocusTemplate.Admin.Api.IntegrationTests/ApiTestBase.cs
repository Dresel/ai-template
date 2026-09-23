namespace FocusTemplate.Admin.Api.IntegrationTests;

// One database per test class (xunit builds one ApiFixture per class) and a Respawn reset before every test, so test
// classes share nothing and run in parallel without a collection. Arrange exactly the rows the test asserts.
public abstract class ApiTestBase(ApiFixture factory) : IClassFixture<ApiFixture>, IAsyncLifetime
{
	protected ApiFixture Factory { get; } = factory;

	public virtual async ValueTask InitializeAsync() => await Factory.ResetAsync();

	public virtual ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}