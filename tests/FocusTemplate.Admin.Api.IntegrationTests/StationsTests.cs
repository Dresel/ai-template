using Microsoft.EntityFrameworkCore;
using SpatialFocus.Primitives;
using SpatialFocus.Weather.Client;
using SpatialFocus.Weather.Client.Stations;
using SpatialFocus.Weather.Data;
using SpatialFocus.Weather.Data.Entities;
using SpatialFocus.Weather.Shared;
using Xunit.Sdk;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// The weather feature through the host: its endpoints, its context beside the spine's, the owner joined in from the
// spine's users and its permission granted through the host's groups.
public sealed class StationsTests(ApiFixture factory) : ApiTestBase(factory)
{
	[Fact]
	public async Task AnEmptyNameIsAValidationProblem()
	{
		Station station = await Factory.AddTestStationAsync();

		StationsRenameResult result = await RenameAsync(station.Id, string.Empty);

		Assert.True(result is ValidationProblem, $"Expected ValidationProblem, got {result}");
	}

	[Fact]
	public async Task AnUnknownStationIsNotFound()
	{
		StationsRenameResult result = await RenameAsync(StationId.New(), "Renamed");

		Assert.True(result is NotFoundProblem, $"Expected NotFoundProblem, got {result}");
	}

	[Fact]
	public async Task ManageStationsRenamesAnyStation()
	{
		UserId owner = (await Factory.AddUserAsync()).Id;
		Station station = await Factory.AddTestStationAsync(owner);
		UserId manager = (await Factory.AddUserAsync("Manager")).Id;
		await Factory.GrantAsync(manager, WeatherPermissions.ManageStations);

		StationsRenameResult result = await RenameAsync(station.Id, "Renamed", manager);

		Assert.Equal("Renamed", Renamed(result).Name);
	}

	[Fact]
	public async Task SomeoneElsesStationIsForbidden()
	{
		UserId owner = (await Factory.AddUserAsync()).Id;
		Station station = await Factory.AddTestStationAsync(owner);

		StationsRenameResult result = await RenameAsync(station.Id, "Renamed");

		Assert.True(result is ForbiddenProblem, $"Expected ForbiddenProblem, got {result}");
		Assert.Equal("Test station", await NameOfAsync(station.Id));
	}

	[Fact]
	public async Task TheListShowsEachStationWithItsOwnersName()
	{
		UserId owner = (await Factory.AddUserAsync("Ada Lovelace")).Id;
		Station station = await Factory.AddTestStationAsync(owner);

		IReadOnlyList<StationResponse> stations =
			await new StationsClient(Factory.CreateAuthenticatedClient()).ListAsync(TestContext.Current.CancellationToken);

		Assert.Equal([new StationResponse(station.Id, "TST", "Test station", owner, "Ada Lovelace"),], stations);
	}

	[Fact]
	public async Task TheOwnerRenamesTheirStation()
	{
		Station station = await Factory.AddTestStationAsync();

		StationsRenameResult result = await RenameAsync(station.Id, "Renamed");

		Assert.Equal("Renamed", Renamed(result).Name);
		Assert.Equal("Renamed", await NameOfAsync(station.Id));
	}

	private static StationResponse Renamed(StationsRenameResult result) =>
		result switch
		{
			StationResponse station => station,
			ValidationProblem problem => throw new XunitException($"Expected the station, got 400: {problem.Problem.Title}"),
			ForbiddenProblem problem => throw new XunitException($"Expected the station, got 403: {problem.Problem.Detail}"),
			NotFoundProblem problem => throw new XunitException($"Expected the station, got 404: {problem.Problem.Detail}"),
		};

	private async Task<string> NameOfAsync(StationId id)
	{
		await using WeatherDbContext dbContext = Factory.CreateWeatherDbContext();

		return await dbContext.Stations.Where(station => station.Id == id)
			.Select(station => station.Name)
			.SingleAsync(TestContext.Current.CancellationToken);
	}

	private Task<StationsRenameResult> RenameAsync(StationId id, string name, UserId? user = null) =>
		new StationsClient(Factory.CreateAuthenticatedClient(user)).RenameAsync(
			id,
			new StationRenameRequest(name),
			TestContext.Current.CancellationToken);
}