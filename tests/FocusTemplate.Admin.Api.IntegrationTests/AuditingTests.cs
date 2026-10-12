using FocusTemplate.Data.Foundation.Auditing;
using FocusTemplate.Primitives;
using FocusTemplate.Weather.Data;
using FocusTemplate.Weather.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class AuditingTests(ApiFixture factory) : ApiTestBase(factory)
{
	private static readonly UserId Editor = UserId.From(new Guid("00000000-0000-7000-8000-00000000ed17"));

	// Two requests in a row rent the same pooled instance. The user is read on every save, so neither carries the
	// other's, which a user captured by the context would.
	[Fact]
	public async Task APooledContextAuditsEachRequestWithItsOwnUser()
	{
		UserId first = (await Factory.AddUserAsync("First")).Id;
		UserId second = (await Factory.AddUserAsync("Second")).Id;

		StationId firstStation = await SaveStationInARequestOfAsync(first, "ONE");
		StationId secondStation = await SaveStationInARequestOfAsync(second, "TWO");

		Assert.Equal(first, (await ReadAuditColumnsAsync(firstStation)).CreatedBy);
		Assert.Equal(second, (await ReadAuditColumnsAsync(secondStation)).CreatedBy);
	}

	[Fact]
	public async Task EditMovesUpdatedAndKeepsCreated()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		Station station = await Factory.AddTestStationAsync();

		Factory.Clock.Advance(TimeSpan.FromHours(1));

		await using (WeatherDbContext dbContext = Factory.CreateWeatherDbContext(Editor))
		{
			Station tracked = await dbContext.Stations.SingleAsync(s => s.Id == station.Id, TestContext.Current.CancellationToken);
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

	// A job such as bootstrap-admin writes through the API's own context, with no request in flight
	[Fact]
	public async Task PooledContextOutsideARequestAuditsAsSystem()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		UserId owner = (await Factory.AddUserAsync()).Id;
		Station station = new()
		{
			Id = StationId.New(),
			OwnerId = owner,
			Code = "JOB",
			Name = "Saved by a job",
			Location = new Point(16.4, 48.2) { SRID = 4326, },
		};

		await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
		{
			scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;

			WeatherDbContext dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
			dbContext.Stations.Add(station);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		Assert.Equal(
			new AuditColumns(createdAt, WellKnownUsers.System, createdAt, WellKnownUsers.System),
			await ReadAuditColumnsAsync(station.Id));
	}

	[Fact]
	public async Task PooledContextTakesTheUserFromTheRequestAndTheClockFromTheHost()
	{
		DateTimeOffset createdAt = Factory.Clock.GetUtcNow();
		UserId owner = (await Factory.AddUserAsync()).Id;
		Station station = new()
		{
			Id = StationId.New(),
			OwnerId = owner,
			Code = "API",
			Name = "Saved by the API's context",
			Location = new Point(16.4, 48.2) { SRID = 4326, },
		};

		await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
		{
			scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
			{
				User = TestAuthenticationHandler.CreatePrincipal(ApiFixture.TestUser), RequestServices = scope.ServiceProvider,
			};

			WeatherDbContext dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
			dbContext.Stations.Add(station);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		Assert.Equal(
			new AuditColumns(createdAt, ApiFixture.TestUser, createdAt, ApiFixture.TestUser),
			await ReadAuditColumnsAsync(station.Id));
	}

	private async Task<AuditColumns> ReadAuditColumnsAsync(StationId id)
	{
		await using WeatherDbContext dbContext = Factory.CreateWeatherDbContext();

		return await dbContext.Stations.Where(s => s.Id == id)
			.Select(s => new AuditColumns(
				EF.Property<DateTimeOffset>(s, AuditingInterceptor.CreatedAt),
				EF.Property<UserId>(s, AuditingInterceptor.CreatedBy),
				EF.Property<DateTimeOffset>(s, AuditingInterceptor.UpdatedAt),
				EF.Property<UserId>(s, AuditingInterceptor.UpdatedBy)))
			.SingleAsync(TestContext.Current.CancellationToken);
	}

	private async Task<StationId> SaveStationInARequestOfAsync(UserId user, string code)
	{
		Station station = new()
		{
			Id = StationId.New(),
			OwnerId = user,
			Code = code,
			Name = $"Saved by {code}",
			Location = new Point(16.4, 48.2) { SRID = 4326, },
		};

		await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
		scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
		{
			User = TestAuthenticationHandler.CreatePrincipal(user), RequestServices = scope.ServiceProvider,
		};

		WeatherDbContext dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
		dbContext.Stations.Add(station);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

		return station.Id;
	}

	private sealed record AuditColumns(DateTimeOffset CreatedAt, UserId CreatedBy, DateTimeOffset UpdatedAt, UserId UpdatedBy);
}