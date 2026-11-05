using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalaryPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    WorkedHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    OvertimeHoursApproved = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    EstimatedAbsentDays = table.Column<int>(type: "integer", nullable: false),
                    LateMinutesTotal = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    HourlyRateUsed = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FixedSalaryUsed = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PositionPercentageUsed = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    OvertimePercentageUsed = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    PositionNameUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RankNameUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OvertimeAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Deduction = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidById = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryPayments_Users_PaidById",
                        column: x => x.PaidById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalaryPayments_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_PaidById",
                table: "SalaryPayments",
                column: "PaidById");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_UserId_Year_Month",
                table: "SalaryPayments",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalaryPayments");
        }
    }
}
