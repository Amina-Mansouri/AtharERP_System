using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class FinanceModulePhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "ProjectExpenses",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "FinancialClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExpenses_SiteId",
                table: "ProjectExpenses",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExpenses_Sites_SiteId",
                table: "ProjectExpenses",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExpenses_Sites_SiteId",
                table: "ProjectExpenses");

            migrationBuilder.DropIndex(
                name: "IX_ProjectExpenses_SiteId",
                table: "ProjectExpenses");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "ProjectExpenses");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "FinancialClaims",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
