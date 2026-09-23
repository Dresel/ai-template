using FocusTemplate.Data;
using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class AuditingTests(ApiFixture factory) : ApiTestBase(factory)
{
	private static readonly UserId Editor = UserId.From(new Guid("00000000-0000-7000-8000-00000000ed17"));

	[Fact]
	public async Task EditMovesUpdatedAndKeepsCreated()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		Station station = await Factory.AddTestStationAsync();

		Factory.Clock.Advance(TimeSpan.FromHours(1));

		await using (AppDbContext dbContext = Factory.CreateDbContext(Editor))
		{
			Station tracked = await dbContext.Stations.SingleAsync(
				s => s.Id == station.Id,
				TestContext.Current.CancellationToken);
			tracked.Name = "Renamed";
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		Assert.Equal(
			new AuditColumns(createdAt, ApiFixture.TestUser, createdAt.AddHours(1), Editor),
			await ReadAuditColumnsAsync(station.Id));
	}

	[Fact]
	public async Task InsertSetsAllFourAuditColumns()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		Station station = await Factory.AddTestStationAsync();

		Assert.Equal(
			new AuditColumns(createdAt, ApiFixture.TestUser, createdAt, ApiFixture.TestUser),
			await ReadAuditColumnsAsync(station.Id));
	}

	[Fact]
	public async Task PooledContextTakesUserAndClockFromTheHost()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		Station station = new()
		{
			Id = StationId.New(),
			OwnerId = ApiFixture.TestUser,
			Code = "API",
			Name = "Saved by the API's context",
			Location = new Point(16.4, 48.2) { SRID = 4326, },
		};

		await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
		{
			AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			dbContext.Stations.Add(station);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		Assert.Equal(
			new AuditColumns(createdAt, ApiFixture.TestUser, createdAt, ApiFixture.TestUser),
			await ReadAuditColumnsAsync(station.Id));
	}

	private async Task<AuditColumns> ReadAuditColumnsAsync(StationId id)
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		return await dbContext.Stations.Where(s => s.Id == id)
			.Select(s => new AuditColumns(
				EF.Property<DateTimeOffset>(s, AuditingInterceptor.CreatedAt),
				EF.Property<UserId>(s, AuditingInterceptor.CreatedBy),
				EF.Property<DateTimeOffset>(s, AuditingInterceptor.UpdatedAt),
				EF.Property<UserId>(s, AuditingInterceptor.UpdatedBy)))
			.SingleAsync(TestContext.Current.CancellationToken);
	}

	private sealed record AuditColumns(
		DateTimeOffset CreatedAt,
		UserId CreatedBy,
		DateTimeOffset UpdatedAt,
		UserId UpdatedBy);
}