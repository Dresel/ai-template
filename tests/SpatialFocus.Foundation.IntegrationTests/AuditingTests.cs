using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpatialFocus.Data;
using SpatialFocus.Data.Entities;
using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;

namespace SpatialFocus.Foundation.IntegrationTests;

// The auditing interceptor on the spine's context pool, which every Admin host registers through user management
public sealed class AuditingTests(ApiFixture factory) : ApiTestBase(factory)
{
	private static readonly UserId Editor = UserId.From(new Guid("00000000-0000-7000-8000-00000000ed17"));

	// Two requests in a row rent the same pooled instance. The user is read on every save, so neither carries the
	// other's, which a user captured by the context would.
	[Fact]
	public async Task APooledContextAuditsEachRequestWithItsOwnUser()
	{
		UserId first = UserId.From(Guid.CreateVersion7());
		UserId second = UserId.From(Guid.CreateVersion7());

		GroupId firstGroup = await SaveGroupInARequestOfAsync(first, "First");
		GroupId secondGroup = await SaveGroupInARequestOfAsync(second, "Second");

		Assert.Equal(first, (await ReadAuditColumnsAsync(firstGroup)).CreatedBy);
		Assert.Equal(second, (await ReadAuditColumnsAsync(secondGroup)).CreatedBy);
	}

	[Fact]
	public async Task EditMovesUpdatedAndKeepsCreated()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		GroupId group = await AddGroupAsync();

		Factory.Clock.Advance(TimeSpan.FromHours(1));

		await using (AppDbContext dbContext = Factory.CreateDbContext(Editor))
		{
			Group tracked = await dbContext.Groups.SingleAsync(g => g.Id == group, TestContext.Current.CancellationToken);
			tracked.Name = "Renamed";
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		Assert.Equal(
			new AuditColumns(createdAt, TestAuthenticationHandler.TestUser, createdAt.AddHours(1), Editor),
			await ReadAuditColumnsAsync(group));
	}

	[Fact]
	public async Task InsertSetsAllFourAuditColumns()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		GroupId group = await AddGroupAsync();

		Assert.Equal(
			new AuditColumns(createdAt, TestAuthenticationHandler.TestUser, createdAt, TestAuthenticationHandler.TestUser),
			await ReadAuditColumnsAsync(group));
	}

	// A job such as bootstrap-admin writes through the API's own context, with no request in flight
	[Fact]
	public async Task PooledContextOutsideARequestAuditsAsSystem()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		Group group = new() { Id = GroupId.New(), Name = "Saved by a job", };

		await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
		{
			scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;

			AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			dbContext.Groups.Add(group);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		Assert.Equal(
			new AuditColumns(createdAt, WellKnownUsers.System, createdAt, WellKnownUsers.System),
			await ReadAuditColumnsAsync(group.Id));
	}

	[Fact]
	public async Task PooledContextTakesTheUserFromTheRequestAndTheClockFromTheHost()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();

		GroupId group = await SaveGroupInARequestOfAsync(TestAuthenticationHandler.TestUser, "Saved by the API's context");

		Assert.Equal(
			new AuditColumns(createdAt, TestAuthenticationHandler.TestUser, createdAt, TestAuthenticationHandler.TestUser),
			await ReadAuditColumnsAsync(group));
	}

	private async Task<GroupId> AddGroupAsync()
	{
		Group group = new() { Id = GroupId.New(), Name = "Audited", };

		await using AppDbContext dbContext = Factory.CreateDbContext();
		dbContext.Groups.Add(group);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

		return group.Id;
	}

	private async Task<AuditColumns> ReadAuditColumnsAsync(GroupId id)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		return await dbContext.Groups.Where(g => g.Id == id)
			.Select(g => new AuditColumns(
				EF.Property<DateTimeOffset>(g, AuditingInterceptor.CreatedAt),
				EF.Property<UserId>(g, AuditingInterceptor.CreatedBy),
				EF.Property<DateTimeOffset>(g, AuditingInterceptor.UpdatedAt),
				EF.Property<UserId>(g, AuditingInterceptor.UpdatedBy)))
			.SingleAsync(TestContext.Current.CancellationToken);
	}

	private async Task<GroupId> SaveGroupInARequestOfAsync(UserId user, string name)
	{
		Group group = new() { Id = GroupId.New(), Name = name, };

		await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
		scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
		{
			User = TestAuthenticationHandler.CreatePrincipal(user), RequestServices = scope.ServiceProvider,
		};

		AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		dbContext.Groups.Add(group);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

		return group.Id;
	}

	private sealed record AuditColumns(DateTimeOffset CreatedAt, UserId CreatedBy, DateTimeOffset UpdatedAt, UserId UpdatedBy);
}