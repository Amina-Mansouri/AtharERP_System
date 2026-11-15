using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceEvaluationUploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ValidationPassed = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationNotes = table.Column<string>(type: "text", nullable: true),
                    UploadedById = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceEvaluationUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceEvaluationUploads_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeMonthlyKpis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerformanceEvaluationUploadId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AvailableWorkingHours = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    ActualWorkingHours = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    TotalFinalProductionScore = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    TotalTasks = table.Column<int>(type: "integer", nullable: true),
                    OnTimeTasks = table.Column<int>(type: "integer", nullable: true),
                    LateTasks = table.Column<int>(type: "integer", nullable: true),
                    EngineeringKpi = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    HrEvaluation = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    FinalMonthlyKpi = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    PerformanceGrade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeMonthlyKpis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeMonthlyKpis_PerformanceEvaluationUploads_Performanc~",
                        column: x => x.PerformanceEvaluationUploadId,
                        principalTable: "PerformanceEvaluationUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeMonthlyKpis_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMonthlyKpis_PerformanceEvaluationUploadId",
                table: "EmployeeMonthlyKpis",
                column: "PerformanceEvaluationUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMonthlyKpis_UserId",
                table: "EmployeeMonthlyKpis",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceEvaluationUploads_UploadedById",
                table: "PerformanceEvaluationUploads",
                column: "UploadedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeMonthlyKpis");

            migrationBuilder.DropTable(
                name: "PerformanceEvaluationUploads");
        }
    }
}
