using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryAdvances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceDeduction",
                table: "SalaryPayments",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SalaryAdvanceRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InstallmentMonths = table.Column<int>(type: "integer", nullable: false),
                    MonthlyInstallment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MonthsRepaid = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedById = table.Column<string>(type: "text", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryAdvanceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryAdvanceRequests_Users_DecidedById",
                        column: x => x.DecidedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalaryAdvanceRequests_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdvanceRequests_DecidedById",
                table: "SalaryAdvanceRequests",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdvanceRequests_UserId",
                table: "SalaryAdvanceRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalaryAdvanceRequests");

            migrationBuilder.DropColumn(
                name: "AdvanceDeduction",
                table: "SalaryPayments");
        }
    }
}
