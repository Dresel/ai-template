using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChainCursors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChainCursors",
                columns: table => new
                {
                    ChainId = table.Column<long>(type: "bigint", nullable: false),
                    LastProcessedBlock = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainCursors", x => x.ChainId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChainCursors");
        }
    }
}
