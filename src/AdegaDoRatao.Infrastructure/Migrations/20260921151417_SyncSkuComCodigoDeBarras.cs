using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdegaDoRatao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncSkuComCodigoDeBarras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SKU passa a ser sempre igual ao código de barras (EAN).
            // Produtos existentes com EAN têm o SKU sincronizado; o histórico
            // (movimentações, vendas, compras) referencia o produto por Id,
            // então é preservado automaticamente.
            migrationBuilder.Sql(
                """
                UPDATE "Products"
                SET "Sku" = UPPER(TRIM("Barcode"))
                WHERE "Barcode" IS NOT NULL
                  AND "Sku" <> UPPER(TRIM("Barcode"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
