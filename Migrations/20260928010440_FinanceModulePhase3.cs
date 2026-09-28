using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class FinanceModulePhase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialRecords_ProjectAssignments_ProjectAssignmentId",
                table: "FinancialRecords");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialRecords_ProjectAssignments_ProjectAssignmentId",
                table: "FinancialRecords",
                column: "ProjectAssignmentId",
                principalTable: "ProjectAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialRecords_ProjectAssignments_ProjectAssignmentId",
                table: "FinancialRecords");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialRecords_ProjectAssignments_ProjectAssignmentId",
                table: "FinancialRecords",
                column: "ProjectAssignmentId",
                principalTable: "ProjectAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
