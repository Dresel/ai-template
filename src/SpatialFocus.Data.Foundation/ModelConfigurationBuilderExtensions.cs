using Microsoft.EntityFrameworkCore;
using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Foundation;

public static class ModelConfigurationBuilderExtensions
{
	public static ModelConfigurationBuilder AddFoundationConventions(this ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.RegisterAllInFoundationEfCoreConverters();

		configurationBuilder.Properties<UserId>().HaveSentinel(UserId.Unspecified);

		return configurationBuilder;
	}
}