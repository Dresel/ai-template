using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTemplate.Data.Foundation;

public static class ServiceProviderExtensions
{
	public static string GetRequiredConnectionString(this IServiceProvider provider, string connectionName) =>
		provider.GetRequiredService<IConfiguration>().GetConnectionString(connectionName) ??
		throw new InvalidOperationException($"Connection string '{connectionName}' not found.");
}