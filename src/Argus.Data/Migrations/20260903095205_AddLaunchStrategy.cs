using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Argus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLaunchStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LaunchStrategyAddress",
                table: "TokenDeployments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaunchStrategyAddress",
                table: "TokenDeployments");
        }
    }
}
