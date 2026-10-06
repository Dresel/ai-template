using FocusTemplate.Data.Auditing;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Data;

// Base class for writable and read-only db contexts.
public abstract class AppDbContextBase(DbContextOptions options) : DbContext(options)
{
	public DbSet<Station> Stations => Set<Station>();

	public DbSet<Observation> Observations => Set<Observation>();

	public DbSet<Alert> Alerts => Set<Alert>();

	public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

	public DbSet<User> Users => Set<User>();

	public DbSet<UserActivity> UserActivities => Set<UserActivity>();

	public DbSet<Group> Groups => Set<Group>();

	public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

	public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

	public DbSet<PermissionDefinition> Permissions => Set<PermissionDefinition>();

	protected sealed override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.RegisterAllInVogenEfCoreConverters();

		configurationBuilder.Properties<UserId>().HaveSentinel(UserId.Unspecified);
		configurationBuilder.Properties<StationId>().HaveSentinel(StationId.Unspecified);
		configurationBuilder.Properties<ObservationId>().HaveSentinel(ObservationId.Unspecified);
		configurationBuilder.Properties<WeatherForecastId>().HaveSentinel(WeatherForecastId.Unspecified);
		configurationBuilder.Properties<AlertId>().HaveSentinel(AlertId.Unspecified);
		configurationBuilder.Properties<GroupId>().HaveSentinel(GroupId.Unspecified);
		configurationBuilder.Properties<Permission>().HaveSentinel(Permission.Unspecified);
	}

	protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// The migration creates the extension, which the postgis/postgis image ships.
		modelBuilder.HasPostgresExtension("postgis");

		modelBuilder.ApplyEntityConfigurations();
		modelBuilder.AddAuditingShadowProperties();
	}
}