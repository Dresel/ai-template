using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Argus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenDeployments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenDeployments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChainId = table.Column<long>(type: "bigint", nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: false),
                    BlockHash = table.Column<string>(type: "text", nullable: false),
                    TransactionHash = table.Column<string>(type: "text", nullable: false),
                    ContractAddress = table.Column<string>(type: "text", nullable: false),
                    DeployerAddress = table.Column<string>(type: "text", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenDeployments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenDeployments_ChainId_TransactionHash",
                table: "TokenDeployments",
                columns: new[] { "ChainId", "TransactionHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenDeployments");
        }
    }
}
