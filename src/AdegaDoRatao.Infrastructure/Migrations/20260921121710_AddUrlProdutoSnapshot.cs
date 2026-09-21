using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdegaDoRatao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlProdutoSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrlProduto",
                table: "MarketPriceSnapshots",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlProduto",
                table: "MarketPriceSnapshots");
        }
    }
}
