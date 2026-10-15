using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SpatialFocus.Data;
using SpatialFocus.Data.Entities;
using SpatialFocus.Primitives;

namespace SpatialFocus.Foundation.IntegrationTests;

// The spine's read-only context from the host's own container, so the registration under test is the one the handlers
// get, on the reader role the AppHost grants
public sealed class ReadOnlyDbContextTests(ApiFixture factory) : ApiTestBase(factory)
{
	// The second scope rents the instance the first one returned to the pool, so the tracking setting is checked
	// after a pool round trip, not only on a freshly constructed context.
	[Fact]
	public async Task QueriesLeaveNothingTrackedAfterAPoolRoundTrip()
	{
		GroupId group = await AddGroupAsync();

		await using (AsyncServiceScope first = Factory.Services.CreateAsyncScope())
		{
			_ = await first.ServiceProvider.GetRequiredService<ReadOnlyAppDbContext>()
				.Groups.SingleAsync(g => g.Id == group, TestContext.Current.CancellationToken);
		}

		await using AsyncServiceScope second = Factory.Services.CreateAsyncScope();
		ReadOnlyAppDbContext dbContext = second.ServiceProvider.GetRequiredService<ReadOnlyAppDbContext>();
		_ = await dbContext.Groups.SingleAsync(g => g.Id == group, TestContext.Current.CancellationToken);

		Assert.Empty(dbContext.ChangeTracker.Entries());
	}

	[Fact]
	public async Task SaveChangesIsRefusedBeforeReachingTheDatabase()
	{
		await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
		ReadOnlyAppDbContext dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyAppDbContext>();

		dbContext.Groups.Add(new Group { Id = GroupId.New(), Name = "Never saved", });

		await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
	}

	// ExecuteDelete never passes SaveChanges, so only the database role stands between it and the rows.
	[Fact]
	public async Task TheReaderRoleRejectsWritesThatBypassSaveChanges()
	{
		GroupId group = await AddGroupAsync();

		await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
		{
			ReadOnlyAppDbContext dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyAppDbContext>();

			_ = await dbContext.Groups.CountAsync(TestContext.Current.CancellationToken);

			PostgresException exception = await Assert.ThrowsAsync<PostgresException>(() =>
				dbContext.Groups.ExecuteDeleteAsync(TestContext.Current.CancellationToken));

			Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
		}

		await using AppDbContext owner = Factory.CreateDbContext();
		Assert.True(await owner.Groups.AnyAsync(g => g.Id == group, TestContext.Current.CancellationToken));
	}

	private async Task<GroupId> AddGroupAsync()
	{
		Group group = new() { Id = GroupId.New(), Name = "Read only", };

		await using AppDbContext dbContext = Factory.CreateDbContext();
		dbContext.Groups.Add(group);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

		return group.Id;
	}
}