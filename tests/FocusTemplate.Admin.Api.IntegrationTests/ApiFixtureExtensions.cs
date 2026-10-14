using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SpatialFocus.Data;
using SpatialFocus.Data.Entities;
using SpatialFocus.Primitives;
using SpatialFocus.Weather.Data;
using SpatialFocus.Weather.Data.Entities;
using SpatialFocus.Weather.Primitives;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public static class ApiFixtureExtensions
{
	// The owner's row first, since the station's foreign key points at the spine's users
	public static async Task<Station> AddTestStationAsync(this ApiFixture factory, UserId? owner = null, string code = "TST")
	{
		UserId ownerId = owner ?? ApiFixture.TestUser;
		await factory.AddOwnerAsync(ownerId);

		Station station = new()
		{
			Id = StationId.New(),
			OwnerId = ownerId,
			Code = code,
			Name = "Test station",
			Location = new Point(16.4, 48.2) { SRID = 4326, },
		};

		await using WeatherDbContext dbContext = factory.CreateWeatherDbContext();
		dbContext.Stations.Add(station);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

		return station;
	}

	private static async Task AddOwnerAsync(this ApiFixture factory, UserId owner)
	{
		CancellationToken cancellationToken = TestContext.Current.CancellationToken;

		await using AppDbContext dbContext = factory.CreateDbContext();
		if (await dbContext.Users.AnyAsync(user => user.Id == owner, cancellationToken))
		{
			return;
		}

		dbContext.Users.Add(new User { Id = owner, DisplayName = "Station owner", FirstSeenAt = factory.Clock.GetUtcNow(), });
		await dbContext.SaveChangesAsync(cancellationToken);
	}
}