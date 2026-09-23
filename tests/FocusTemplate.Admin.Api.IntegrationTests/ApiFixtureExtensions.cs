using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using NetTopologySuite.Geometries;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public static class ApiFixtureExtensions
{
	public static async Task<Station> AddTestStationAsync(this ApiFixture factory)
	{
		Station station = new()
		{
			Id = StationId.New(),
			OwnerId = ApiFixture.TestUser,
			Code = "TST",
			Name = "Test station",
			Location = new Point(16.4, 48.2) { SRID = 4326, },
		};

		await using AppDbContext dbContext = factory.CreateDbContext();
		dbContext.Stations.Add(station);
		await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

		return station;
	}
}