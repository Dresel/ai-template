using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Npgsql;
using SpatialFocus.Primitives;
using SpatialFocus.Weather.Data;
using SpatialFocus.Weather.Data.Entities;
using SpatialFocus.Weather.Primitives;

namespace SpatialFocus.Weather.IntegrationTests;

// The feature's read-only context from the host's own container, so the registration under test is the one its handlers
// get, on the reader role granted on the feature's schema
public sealed class ReadOnlyDbContextTests(ApiFixture factory) : ApiTestBase(factory)
{
	// The second scope rents the instance the first one returned to the pool, so the tracking setting is checked
	// after a pool round trip, not only on a freshly constructed context.
	[Fact]
	public async Task QueriesLeaveNothingTrackedAfterAPoolRoundTrip()
	{
		Station station = await Factory.AddTestStationAsync();

		await using (AsyncServiceScope first = Factory.Services.CreateAsyncScope())
		{
			_ = await first.ServiceProvider.GetRequiredService<ReadOnlyWeatherDbContext>()
				.Stations.SingleAsync(s => s.Id == station.Id, TestContext.Current.CancellationToken);
		}

		await using AsyncServiceScope second = Factory.Services.CreateAsyncScope();
		ReadOnlyWeatherDbContext dbContext = second.ServiceProvider.GetRequiredService<ReadOnlyWeatherDbContext>();
		_ = await dbContext.Stations.SingleAsync(s => s.Id == station.Id, TestContext.Current.CancellationToken);

		Assert.Empty(dbContext.ChangeTracker.Entries());
	}

	[Fact]
	public async Task SaveChangesIsRefusedBeforeReachingTheDatabase()
	{
		await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
		ReadOnlyWeatherDbContext dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyWeatherDbContext>();

		dbContext.Stations.Add(
			new Station
			{
				Id = StationId.New(),
				OwnerId = TestAuthenticationHandler.TestUser,
				Code = "RO",
				Name = "Never saved",
				Location = new Point(16.4, 48.2) { SRID = 4326, },
			});

		await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
	}

	// ExecuteDelete never passes SaveChanges, so only the database role stands between it and the rows.
	[Fact]
	public async Task TheReaderRoleRejectsWritesThatBypassSaveChanges()
	{
		Station station = await Factory.AddTestStationAsync();

		await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
		{
			ReadOnlyWeatherDbContext dbContext = scope.ServiceProvider.GetRequiredService<ReadOnlyWeatherDbContext>();

			PostgresException exception = await Assert.ThrowsAsync<PostgresException>(() =>
				dbContext.Stations.ExecuteDeleteAsync(TestContext.Current.CancellationToken));

			Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
		}

		await using WeatherDbContext owner = Factory.CreateWeatherDbContext();
		Assert.True(await owner.Stations.AnyAsync(s => s.Id == station.Id, TestContext.Current.CancellationToken));
	}
}