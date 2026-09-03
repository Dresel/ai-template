using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Argus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPonsLaunchCalldata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<BigInteger>(
                name: "CreatorBuyQuote",
                table: "TokenDeployments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PairTokenAddress",
                table: "TokenDeployments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TokenDeploymentInsiders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TokenDeploymentId = table.Column<long>(type: "bigint", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenDeploymentInsiders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenDeploymentInsiders_TokenDeployments_TokenDeploymentId",
                        column: x => x.TokenDeploymentId,
                        principalTable: "TokenDeployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenDeploymentInsiders_TokenDeploymentId_Address",
                table: "TokenDeploymentInsiders",
                columns: new[] { "TokenDeploymentId", "Address" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenDeploymentInsiders");

            migrationBuilder.DropColumn(
                name: "CreatorBuyQuote",
                table: "TokenDeployments");

            migrationBuilder.DropColumn(
                name: "PairTokenAddress",
                table: "TokenDeployments");
        }
    }
}
