using System.Globalization;
using Argus.Api;
using Argus.Data;
using Argus.Shared;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<AppDbContext>("argusdb");

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.MapGet(
		"/tokendeployments",
		async (
			AppDbContext dbContext,
			CancellationToken cancellationToken,
			bool tokensOnly = false,
			string? search = null) =>
		{
			IQueryable<Argus.Data.Entities.TokenDeployment> query = dbContext.TokenDeployments;

			if (tokensOnly)
			{
				query = query.Where(entity => entity.TokenSymbol != null);
			}

			if (!string.IsNullOrWhiteSpace(search))
			{
				// One box for every identifier a launch is known by: symbol, name, or any of its
				// addresses/hashes. ILIKE keeps it case-insensitive in Postgres; the wildcards a
				// user may type are escaped so they match literally.
				string pattern = $"%{search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
					.Replace("%", "\\%", StringComparison.Ordinal)
					.Replace("_", "\\_", StringComparison.Ordinal)}%";

				query = query.Where(entity =>
					(entity.TokenSymbol != null && EF.Functions.ILike(entity.TokenSymbol, pattern)) ||
					(entity.TokenName != null && EF.Functions.ILike(entity.TokenName, pattern)) ||
					EF.Functions.ILike(entity.ContractAddress, pattern) ||
					EF.Functions.ILike(entity.DeployerAddress, pattern) ||
					EF.Functions.ILike(entity.TransactionHash, pattern) || (entity.FactoryAddress != null &&
						EF.Functions.ILike(entity.FactoryAddress, pattern)));
			}

			var rows = await query.OrderByDescending(entity => entity.DetectedAt)
				.ThenByDescending(entity => entity.Id)
				.Take(100)
				.Select(entity => new
				{
					entity.ChainId,
					entity.ContractAddress,
					entity.DeployerAddress,
					entity.FactoryAddress,
					entity.LaunchpadName,
					entity.TokenName,
					entity.TokenSymbol,
					entity.TokenDecimals,
					entity.PairTokenAddress,
					entity.CreatorBuyQuote,
					entity.LaunchStrategyAddress,
					InsiderCount = entity.Insiders.Count,
					entity.TransactionHash,
					entity.BlockNumber,
					entity.DetectedAt,
				})
				.ToArrayAsync(cancellationToken);

			// The serial-launcher rule needs each deployer's token launches across the whole
			// archive, not just this page - one grouped query for the page's deployers.
			string[] deployers = [.. rows.Select(row => row.DeployerAddress).Distinct(),];
			Dictionary<string, int> launchesByDeployer = await dbContext.TokenDeployments
				.Where(entity => deployers.Contains(entity.DeployerAddress) && entity.TokenSymbol != null)
				.GroupBy(entity => entity.DeployerAddress)
				.Select(group => new { Deployer = group.Key, Count = group.Count(), })
				.ToDictionaryAsync(group => group.Deployer, group => group.Count, cancellationToken);

			// The uint256 is stringified after materialization - EF cannot translate a
			// culture-aware BigInteger.ToString into SQL.
			TokenDeploymentResponse[] deployments =
			[
				.. rows.Select(row =>
				{
					int otherLaunches = Math.Max(
						0,
						launchesByDeployer.GetValueOrDefault(row.DeployerAddress) - (row.TokenSymbol is null ? 0 : 1));

					LaunchAssessmentResult assessment = LaunchAssessment.Assess(
						new LaunchFacts(
							row.LaunchpadName,
							row.InsiderCount,
							row.PairTokenAddress,
							row.CreatorBuyQuote,
							otherLaunches,
							row.LaunchStrategyAddress));

					return new TokenDeploymentResponse(
						row.ChainId,
						row.ContractAddress,
						row.DeployerAddress,
						row.FactoryAddress,
						row.LaunchpadName,
						row.TokenName,
						row.TokenSymbol,
						row.TokenDecimals,
						row.PairTokenAddress,
						row.CreatorBuyQuote?.ToString(CultureInfo.InvariantCulture),
						row.InsiderCount,
						row.TransactionHash,
						row.BlockNumber,
						row.DetectedAt,
						assessment.Verdict,
						LaunchAssessment.RulesVersion,
						assessment.Flags);
				}),
			];

			return deployments;
		})
	.WithName("GetTokenDeployments");

app.Run();