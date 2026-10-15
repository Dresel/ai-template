using Microsoft.Extensions.Time.Testing;
using SpatialFocus.Data;
using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;

namespace SpatialFocus.Testing.Api;

// What the arrangements of user management and the features need of a fixture, whichever host it runs
public interface IApiFixture
{
	FakeTimeProvider Clock { get; }

	// The owner's, which the API gets under its database connection name
	string ConnectionString { get; }

	// The permissions the host includes, which its schema holds as rows
	IReadOnlyList<Permission> Permissions { get; }

	AuditingInterceptor CreateAuditing(UserId? user = null);

	AppDbContext CreateDbContext(UserId? user = null);

	Task ResetAsync();
}