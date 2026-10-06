namespace FocusTemplate.Admin.Api.Chaos;

public static class RouteGroupBuilderExtensions
{
	private const string SectionName = "Chaos";

	public static RouteGroupBuilder AddChaosFilter(this RouteGroupBuilder endpoints, IConfiguration configuration)
	{
		IConfigurationSection section = configuration.GetSection(SectionName);

		if (section.Exists())
		{
			endpoints.AddEndpointFilter(ChaosFilter.FromConfiguration(section));
		}

		return endpoints;
	}
}