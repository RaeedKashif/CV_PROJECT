using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChequePrintingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredUniqueIndexesForSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cheques_ChequeNumber",
                table: "Cheques");

            migrationBuilder.DropIndex(
                name: "IX_Banks_Name",
                table: "Banks");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_AccountNumber",
                table: "BankAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_ChequeNumber",
                table: "Cheques",
                column: "ChequeNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Banks_Name",
                table: "Banks",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_AccountNumber",
                table: "BankAccounts",
                column: "AccountNumber",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cheques_ChequeNumber",
                table: "Cheques");

            migrationBuilder.DropIndex(
                name: "IX_Banks_Name",
                table: "Banks");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_AccountNumber",
                table: "BankAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_ChequeNumber",
                table: "Cheques",
                column: "ChequeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Banks_Name",
                table: "Banks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_AccountNumber",
                table: "BankAccounts",
                column: "AccountNumber",
                unique: true);
        }
    }
}
