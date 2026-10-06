using FocusTemplate.Admin.Client.Users;
using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class UserProvisioningTests(ApiFixture factory) : ApiTestBase(factory)
{
	// TestAuthenticationHandler names the user "user-<id>" in preferred_username, as Keycloak would its login
	[Fact]
	public async Task TheFirstRequestRecordsTheUserFromTheirToken()
	{
		UserId id = UserId.From(Guid.CreateVersion7());

		await new UsersClient(Factory.CreateAuthenticatedClient(id)).MeAsync(TestContext.Current.CancellationToken);

		User user = await Factory.FindUserAsync(id);
		Assert.Equal($"user-{id.Value:N}", user.DisplayName);
		Assert.True(user.IsActive);
		Assert.Equal(Factory.Clock.GetUtcNow(), user.FirstSeenAt);
		Assert.Equal(Factory.Clock.GetUtcNow(), await LastSeenAtAsync(id));
	}

	[Fact]
	public async Task ActivityIsRecordedAtMostOncePerQuarterOfAnHour()
	{
		UserId id = UserId.From(Guid.CreateVersion7());
		UsersClient client = new(Factory.CreateAuthenticatedClient(id));
		await client.MeAsync(TestContext.Current.CancellationToken);
		DateTimeOffset first = Factory.Clock.GetUtcNow();

		Factory.Clock.Advance(TimeSpan.FromMinutes(10));
		await client.MeAsync(TestContext.Current.CancellationToken);
		DateTimeOffset withinTheWindow = await LastSeenAtAsync(id);

		Factory.Clock.Advance(TimeSpan.FromMinutes(10));
		await client.MeAsync(TestContext.Current.CancellationToken);

		Assert.Equal(first, withinTheWindow);
		Assert.Equal(Factory.Clock.GetUtcNow(), await LastSeenAtAsync(id));
	}

	// The activity has a table of its own, so "last updated" of the user stays a change somebody made
	[Fact]
	public async Task AVisitLeavesTheAuditColumnsOfTheUserAlone()
	{
		UserId id = UserId.From(Guid.CreateVersion7());
		UsersClient client = new(Factory.CreateAuthenticatedClient(id));
		await client.MeAsync(TestContext.Current.CancellationToken);
		DateTimeOffset created = await UpdatedAtAsync(id);

		Factory.Clock.Advance(TimeSpan.FromHours(1));
		await client.MeAsync(TestContext.Current.CancellationToken);

		Assert.Equal(created, await UpdatedAtAsync(id));
	}

	private async Task<DateTimeOffset> LastSeenAtAsync(UserId id)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		return await dbContext.UserActivities.Where(activity => activity.UserId == id)
			.Select(activity => activity.LastSeenAt)
			.SingleAsync(TestContext.Current.CancellationToken);
	}

	private async Task<DateTimeOffset> UpdatedAtAsync(UserId id)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		return await dbContext.Users.Where(user => user.Id == id)
			.Select(user => EF.Property<DateTimeOffset>(user, AuditingInterceptor.UpdatedAt))
			.SingleAsync(TestContext.Current.CancellationToken);
	}
}