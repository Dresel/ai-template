using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LaunchpadName",
                table: "TokenDeployments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenDecimals",
                table: "TokenDeployments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenName",
                table: "TokenDeployments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenSymbol",
                table: "TokenDeployments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaunchpadName",
                table: "TokenDeployments");

            migrationBuilder.DropColumn(
                name: "TokenDecimals",
                table: "TokenDeployments");

            migrationBuilder.DropColumn(
                name: "TokenName",
                table: "TokenDeployments");

            migrationBuilder.DropColumn(
                name: "TokenSymbol",
                table: "TokenDeployments");
        }
    }
}
