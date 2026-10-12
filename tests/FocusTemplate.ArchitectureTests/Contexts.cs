using FocusTemplate.Data;
using FocusTemplate.Weather.Data;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.ArchitectureTests;

// The read-only contexts as the APIs configure them, for their models alone: nothing connects
internal static class Contexts
{
	private const string ConnectionString = "Host=localhost";

	public static DbContext ReadOnly(string model) =>
		model switch
		{
			"spine" => new ReadOnlyAppDbContext(
				new DbContextOptionsBuilder<ReadOnlyAppDbContext>().ConfigureReadOnlyAppDbContext(ConnectionString).Options),
			"weather" => new ReadOnlyWeatherDbContext(
				new DbContextOptionsBuilder<ReadOnlyWeatherDbContext>().ConfigureReadOnlyWeatherDbContext(ConnectionString).Options),
			_ => throw new ArgumentOutOfRangeException(nameof(model), model, "No such model."),
		};
}