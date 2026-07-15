namespace FocusTemplate.Admin.Api.IntegrationTests;

/// <summary>
/// Base class for API integration tests. Each test class gets its own database (via
/// <see cref="ApiFixture"/>), and every test starts on an empty, migrated schema - arrange exactly
/// the rows the test asserts through <see cref="ApiFixture.CreateDbContext"/>.
/// </summary>
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