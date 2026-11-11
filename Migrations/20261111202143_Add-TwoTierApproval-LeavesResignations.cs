using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoTierApprovalLeavesResignations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GmDecidedAt",
                table: "ResignationRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GmDecidedById",
                table: "ResignationRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GmDecidedAt",
                table: "LeaveRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GmDecidedById",
                table: "LeaveRequests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResignationRequests_GmDecidedById",
                table: "ResignationRequests",
                column: "GmDecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_GmDecidedById",
                table: "LeaveRequests",
                column: "GmDecidedById");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_GmDecidedById",
                table: "LeaveRequests",
                column: "GmDecidedById",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResignationRequests_Users_GmDecidedById",
                table: "ResignationRequests",
                column: "GmDecidedById",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_GmDecidedById",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ResignationRequests_Users_GmDecidedById",
                table: "ResignationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ResignationRequests_GmDecidedById",
                table: "ResignationRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_GmDecidedById",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "GmDecidedAt",
                table: "ResignationRequests");

            migrationBuilder.DropColumn(
                name: "GmDecidedById",
                table: "ResignationRequests");

            migrationBuilder.DropColumn(
                name: "GmDecidedAt",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "GmDecidedById",
                table: "LeaveRequests");
        }
    }
}
