using Microsoft.EntityFrameworkCore;
using SpatialFocus.Data.Entities;
using SpatialFocus.Data.Foundation;
using SpatialFocus.Primitives;

namespace SpatialFocus.Data;

// Base class for writable and read-only db contexts.
public abstract class AppDbContextBase(DbContextOptions options) : FoundationDbContext<AppDbContextBase>(options), IHasSchema
{
	public static string Schema => DefaultSchema;

	public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

	public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

	public DbSet<Group> Groups => Set<Group>();

	public DbSet<PermissionDefinition> Permissions => Set<PermissionDefinition>();

	public DbSet<UserActivity> UserActivities => Set<UserActivity>();

	public DbSet<User> Users => Set<User>();

	protected sealed override void ConfigureIds(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.RegisterAllInVogenEfCoreConverters();

		configurationBuilder.Properties<GroupId>().HaveSentinel(GroupId.Unspecified);
		configurationBuilder.Properties<Permission>().HaveSentinel(Permission.Unspecified);
	}

	protected sealed override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ApplyEntityConfigurations();
}