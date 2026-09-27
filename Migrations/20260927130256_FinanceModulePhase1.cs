using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class FinanceModulePhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerMeter",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "CostType",
                table: "FinancialRecords");

            migrationBuilder.DropColumn(
                name: "ClientApprovedAt",
                table: "FinancialClaims");

            migrationBuilder.RenameColumn(
                name: "TechnicalApprovedAt",
                table: "FinancialClaims",
                newName: "ClientSettledAt");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "FinancialClaims",
                newName: "ProjectAssignmentId");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SiteSupplyRequests",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "SiteContractors",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleMarkupPercent1",
                table: "ProjectStages",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleMarkupPercent2",
                table: "ProjectStages",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleValue",
                table: "ProjectStages",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Area",
                table: "ProjectAssignments",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMeter",
                table: "ProjectAssignments",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePricePerMeter",
                table: "ProjectAssignments",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "JobRanks",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectAssignmentId",
                table: "FinancialRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClearedAt",
                table: "FinancialRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContributionPercentage",
                table: "FinancialRecords",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineerId",
                table: "FinancialRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMeter",
                table: "FinancialRecords",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Area",
                table: "FinancialClaims",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClientSettled",
                table: "FinancialClaims",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RedesignIncreasePercentage",
                table: "FinancialClaims",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleMarkupPercent1",
                table: "FinancialClaims",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleMarkupPercent2",
                table: "FinancialClaims",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePricePerMeter",
                table: "FinancialClaims",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContributionPercentage",
                table: "AssignmentEngineers",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClientRedesignRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectStageId = table.Column<int>(type: "integer", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IncreasePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRedesignRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientRedesignRequests_ProjectStages_ProjectStageId",
                        column: x => x.ProjectStageId,
                        principalTable: "ProjectStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NameAr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    ExpenseCategoryId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectExpenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectExpenses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecords_EngineerId",
                table: "FinancialRecords",
                column: "EngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialClaims_ProjectAssignmentId",
                table: "FinancialClaims",
                column: "ProjectAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientRedesignRequests_ProjectStageId",
                table: "ClientRedesignRequests",
                column: "ProjectStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExpenses_ExpenseCategoryId",
                table: "ProjectExpenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectExpenses_ProjectId",
                table: "ProjectExpenses",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialClaims_ProjectAssignments_ProjectAssignmentId",
                table: "FinancialClaims",
                column: "ProjectAssignmentId",
                principalTable: "ProjectAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialRecords_Users_EngineerId",
                table: "FinancialRecords",
                column: "EngineerId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialClaims_ProjectAssignments_ProjectAssignmentId",
                table: "FinancialClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialRecords_Users_EngineerId",
                table: "FinancialRecords");

            migrationBuilder.DropTable(
                name: "ClientRedesignRequests");

            migrationBuilder.DropTable(
                name: "ProjectExpenses");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_FinancialRecords_EngineerId",
                table: "FinancialRecords");

            migrationBuilder.DropIndex(
                name: "IX_FinancialClaims_ProjectAssignmentId",
                table: "FinancialClaims");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SiteSupplyRequests");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "SiteContractors");

            migrationBuilder.DropColumn(
                name: "SaleMarkupPercent1",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "SaleMarkupPercent2",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "SaleValue",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "PricePerMeter",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "SalePricePerMeter",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "JobRanks");

            migrationBuilder.DropColumn(
                name: "ClearedAt",
                table: "FinancialRecords");

            migrationBuilder.DropColumn(
                name: "ContributionPercentage",
                table: "FinancialRecords");

            migrationBuilder.DropColumn(
                name: "EngineerId",
                table: "FinancialRecords");

            migrationBuilder.DropColumn(
                name: "PricePerMeter",
                table: "FinancialRecords");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "FinancialClaims");

            migrationBuilder.DropColumn(
                name: "IsClientSettled",
                table: "FinancialClaims");

            migrationBuilder.DropColumn(
                name: "RedesignIncreasePercentage",
                table: "FinancialClaims");

            migrationBuilder.DropColumn(
                name: "SaleMarkupPercent1",
                table: "FinancialClaims");

            migrationBuilder.DropColumn(
                name: "SaleMarkupPercent2",
                table: "FinancialClaims");

            migrationBuilder.DropColumn(
                name: "SalePricePerMeter",
                table: "FinancialClaims");

            migrationBuilder.DropColumn(
                name: "ContributionPercentage",
                table: "AssignmentEngineers");

            migrationBuilder.RenameColumn(
                name: "ProjectAssignmentId",
                table: "FinancialClaims",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ClientSettledAt",
                table: "FinancialClaims",
                newName: "TechnicalApprovedAt");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMeter",
                table: "ProjectStages",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectAssignmentId",
                table: "FinancialRecords",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CostType",
                table: "FinancialRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClientApprovedAt",
                table: "FinancialClaims",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
