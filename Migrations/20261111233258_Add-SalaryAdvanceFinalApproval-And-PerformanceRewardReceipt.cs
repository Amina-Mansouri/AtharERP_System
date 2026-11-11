using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryAdvanceFinalApprovalAndPerformanceRewardReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GmDecidedAt",
                table: "SalaryAdvanceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GmDecidedById",
                table: "SalaryAdvanceRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "SalaryAdvanceRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "ProjectExpenses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineerId",
                table: "ProjectExpenses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "ProjectExpenses",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdvanceRequests_GmDecidedById",
                table: "SalaryAdvanceRequests",
                column: "GmDecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExpenses_CreatedById",
                table: "ProjectExpenses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExpenses_EngineerId",
                table: "ProjectExpenses",
                column: "EngineerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExpenses_Users_CreatedById",
                table: "ProjectExpenses",
                column: "CreatedById",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExpenses_Users_EngineerId",
                table: "ProjectExpenses",
                column: "EngineerId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryAdvanceRequests_Users_GmDecidedById",
                table: "SalaryAdvanceRequests",
                column: "GmDecidedById",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExpenses_Users_CreatedById",
                table: "ProjectExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExpenses_Users_EngineerId",
                table: "ProjectExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryAdvanceRequests_Users_GmDecidedById",
                table: "SalaryAdvanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalaryAdvanceRequests_GmDecidedById",
                table: "SalaryAdvanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ProjectExpenses_CreatedById",
                table: "ProjectExpenses");

            migrationBuilder.DropIndex(
                name: "IX_ProjectExpenses_EngineerId",
                table: "ProjectExpenses");

            migrationBuilder.DropColumn(
                name: "GmDecidedAt",
                table: "SalaryAdvanceRequests");

            migrationBuilder.DropColumn(
                name: "GmDecidedById",
                table: "SalaryAdvanceRequests");

            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "SalaryAdvanceRequests");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ProjectExpenses");

            migrationBuilder.DropColumn(
                name: "EngineerId",
                table: "ProjectExpenses");

            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "ProjectExpenses");
        }
    }
}
