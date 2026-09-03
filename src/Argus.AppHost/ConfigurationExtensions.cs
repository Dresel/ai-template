using Microsoft.Extensions.Configuration.Json;

namespace Argus.AppHost;

internal static class ConfigurationExtensions
{
	public static IDistributedApplicationBuilder AddLocalSettings(this IDistributedApplicationBuilder builder)
	{
		int index = builder.Configuration.Sources.Select((source, index) => new { Source = source, Index = index, })
			.LastOrDefault(x => x.Source is JsonConfigurationSource)
			?.Index ?? -1;

		// Add after the last JSON source (appsettings.json or appsettings.{Environment}.json) so that it overrides them,
		// but before the environment-variable and command-line sources so that those still take precedence
		builder.Configuration.Sources.Insert(
			index,
			new JsonConfigurationSource { Path = "appsettings.local.json", Optional = true, ReloadOnChange = true, });

		return builder;
	}
}