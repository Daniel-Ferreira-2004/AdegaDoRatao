using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdegaDoRatao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Purchases_CreatedByUserId",
                table: "Purchases",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_PaymentMethodId",
                table: "FinancialTransactions",
                column: "PaymentMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_PaymentMethods_PaymentMethodId",
                table: "FinancialTransactions",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Users_CreatedByUserId",
                table: "Purchases",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_PaymentMethods_PaymentMethodId",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Users_CreatedByUserId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_CreatedByUserId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_PaymentMethodId",
                table: "FinancialTransactions");
        }
    }
}
