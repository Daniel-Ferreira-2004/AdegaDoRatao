using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdegaDoRatao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiancaTipoPrecoSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Confianca",
                table: "MarketPriceSnapshots",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RegiaoConfirmada",
                table: "MarketPriceSnapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TipoPreco",
                table: "MarketPriceSnapshots",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confianca",
                table: "MarketPriceSnapshots");

            migrationBuilder.DropColumn(
                name: "RegiaoConfirmada",
                table: "MarketPriceSnapshots");

            migrationBuilder.DropColumn(
                name: "TipoPreco",
                table: "MarketPriceSnapshots");
        }
    }
}
