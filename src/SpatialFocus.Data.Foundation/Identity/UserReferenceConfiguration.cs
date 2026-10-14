using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SpatialFocus.Data.Foundation.Identity;

// The spine's migrations own the table, so a feature's migrations only point their foreign keys at it
public sealed class UserReferenceConfiguration : IEntityTypeConfiguration<UserReference>
{
	public const string Schema = "public";

	public const string Table = "users";

	public void Configure(EntityTypeBuilder<UserReference> builder)
	{
		builder.ToTable(Table, Schema, table => table.ExcludeFromMigrations());
		builder.HasKey(user => user.Id);
	}
}