using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceEvaluationFullDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdjustedProductionTarget",
                table: "EmployeeMonthlyKpis",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedLeaveHours",
                table: "EmployeeMonthlyKpis",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedTasks",
                table: "EmployeeMonthlyKpis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discipline",
                table: "EmployeeMonthlyKpis",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EarlyTasks",
                table: "EmployeeMonthlyKpis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EngineeringKpiWeight",
                table: "EmployeeMonthlyKpis",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HrEvaluationWeight",
                table: "EmployeeMonthlyKpis",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OnTimeDeliveryRatePercent",
                table: "EmployeeMonthlyKpis",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingTasks",
                table: "EmployeeMonthlyKpis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductionAchievementPercent",
                table: "EmployeeMonthlyKpis",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecognizedProductionWithinPeriod",
                table: "EmployeeMonthlyKpis",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardProductionTarget",
                table: "EmployeeMonthlyKpis",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardWorkingHours",
                table: "EmployeeMonthlyKpis",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamDepartment",
                table: "EmployeeMonthlyKpis",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBaseProduction",
                table: "EmployeeMonthlyKpis",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRevisionDeduction",
                table: "EmployeeMonthlyKpis",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTasksCount",
                table: "EmployeeMonthlyKpis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkingHoursUtilizationPercent",
                table: "EmployeeMonthlyKpis",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerformanceTaskDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerformanceEvaluationUploadId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProjectNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProjectName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProjectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Discipline = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuildingNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BuildingName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TaskStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Element = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuildingArea = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ProjectTypeFactor = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ScopeWeight = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ElementWeight = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    BaseProduction = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    DeadlineStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeadlineFactor = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    RevisionCount = table.Column<int>(type: "integer", nullable: true),
                    RevisionReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EligibleRevisions = table.Column<int>(type: "integer", nullable: true),
                    EligibilityReduction = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    RevisionCompliance = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    FinalTaskScore = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    TaskStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TaskDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalTaskDays = table.Column<int>(type: "integer", nullable: true),
                    DailyBaseProduction = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    DaysWithinReportPeriod = table.Column<int>(type: "integer", nullable: true),
                    RecognizedProductionWithinPeriod = table.Column<decimal>(type: "numeric(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceTaskDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceTaskDetails_PerformanceEvaluationUploads_Perform~",
                        column: x => x.PerformanceEvaluationUploadId,
                        principalTable: "PerformanceEvaluationUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerformanceTaskDetails_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceTaskDetails_PerformanceEvaluationUploadId",
                table: "PerformanceTaskDetails",
                column: "PerformanceEvaluationUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceTaskDetails_UserId",
                table: "PerformanceTaskDetails",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceTaskDetails");

            migrationBuilder.DropColumn(
                name: "AdjustedProductionTarget",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "ApprovedLeaveHours",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "CompletedTasks",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "Discipline",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "EarlyTasks",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "EngineeringKpiWeight",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "HrEvaluationWeight",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "OnTimeDeliveryRatePercent",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "PendingTasks",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "ProductionAchievementPercent",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "RecognizedProductionWithinPeriod",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "StandardProductionTarget",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "StandardWorkingHours",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "TeamDepartment",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "TotalBaseProduction",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "TotalRevisionDeduction",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "TotalTasksCount",
                table: "EmployeeMonthlyKpis");

            migrationBuilder.DropColumn(
                name: "WorkingHoursUtilizationPercent",
                table: "EmployeeMonthlyKpis");
        }
    }
}
