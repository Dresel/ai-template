using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFactoryDeployments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TokenDeployments_ChainId_TransactionHash",
                table: "TokenDeployments");

            migrationBuilder.AddColumn<string>(
                name: "FactoryAddress",
                table: "TokenDeployments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenDeployments_ChainId_ContractAddress",
                table: "TokenDeployments",
                columns: new[] { "ChainId", "ContractAddress" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TokenDeployments_ChainId_ContractAddress",
                table: "TokenDeployments");

            migrationBuilder.DropColumn(
                name: "FactoryAddress",
                table: "TokenDeployments");

            migrationBuilder.CreateIndex(
                name: "IX_TokenDeployments_ChainId_TransactionHash",
                table: "TokenDeployments",
                columns: new[] { "ChainId", "TransactionHash" },
                unique: true);
        }
    }
}
