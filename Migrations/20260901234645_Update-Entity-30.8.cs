using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntity308 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialRecords_ProjectCosts_ProjectCostId",
                table: "FinancialRecords");

            migrationBuilder.DropTable(
                name: "ProjectCostSubtasks");

            migrationBuilder.DropTable(
                name: "ProjectCosts");

            migrationBuilder.DropIndex(
                name: "IX_Users_PersonalId",
                schema: "identity",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "PersonalId",
                schema: "identity",
                table: "Users",
                newName: "NextOfKinPhone");

            migrationBuilder.RenameColumn(
                name: "ProjectCostId",
                table: "FinancialRecords",
                newName: "ProjectAssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_FinancialRecords_ProjectCostId",
                table: "FinancialRecords",
                newName: "IX_FinancialRecords_ProjectAssignmentId");

            migrationBuilder.AddColumn<string>(
                name: "ContractImagePath",
                schema: "identity",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                schema: "identity",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SuspendedReason",
                schema: "identity",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLead",
                table: "TaskAssignees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ActualDays",
                table: "ProjectTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpectedDays",
                table: "ProjectTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutputType",
                table: "ProjectTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualDeliveryDate",
                table: "ProjectStages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Area",
                table: "ProjectStages",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMeter",
                table: "ProjectStages",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Projects",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualDeliveryDate",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Custodies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HolderId = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: true),
                    HandedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Custodies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Custodies_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Custodies_Users_HolderId",
                        column: x => x.HolderId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DesignProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    PreparedById = table.Column<string>(type: "text", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClientReply = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignProposals_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DesignProposals_Users_PreparedById",
                        column: x => x.PreparedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    PreparedById = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentRevisions_ProjectDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "ProjectDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentRevisions_Users_PreparedById",
                        column: x => x.PreparedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TechnicalApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClientApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsTransferredToFinance = table.Column<bool>(type: "boolean", nullable: false),
                    TransferredToFinanceAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialClaims_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinancialClaims_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    StageId = table.Column<int>(type: "integer", nullable: true),
                    CostType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Area = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PricePerMeter = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountOrAdditionPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsUrgent = table.Column<bool>(type: "boolean", nullable: false),
                    LeadEngineerId = table.Column<string>(type: "text", nullable: true),
                    AssistantEngineerId = table.Column<string>(type: "text", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AgreedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsTransferredToFinance = table.Column<bool>(type: "boolean", nullable: false),
                    TransferredToFinanceAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAssignments_ProjectStages_StageId",
                        column: x => x.StageId,
                        principalTable: "ProjectStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectAssignments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectAssignments_Users_AssistantEngineerId",
                        column: x => x.AssistantEngineerId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectAssignments_Users_LeadEngineerId",
                        column: x => x.LeadEngineerId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StageTaskTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectStageId = table.Column<int>(type: "integer", nullable: false),
                    TaskName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ExpectedDays = table.Column<int>(type: "integer", nullable: true),
                    OutputType = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageTaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageTaskTemplates_ProjectStages_ProjectStageId",
                        column: x => x.ProjectStageId,
                        principalTable: "ProjectStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    StageId = table.Column<int>(type: "integer", nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Request = table.Column<string>(type: "text", nullable: false),
                    RequestedById = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RoutedToDepartmentId = table.Column<int>(type: "integer", nullable: true),
                    ImpactOnExecution = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalRequests_Departments_RoutedToDepartmentId",
                        column: x => x.RoutedToDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TechnicalRequests_ProjectStages_StageId",
                        column: x => x.StageId,
                        principalTable: "ProjectStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TechnicalRequests_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicalRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAssignmentSubtasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAssignmentSubtasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAssignmentSubtasks_ProjectAssignments_ProjectAssignm~",
                        column: x => x.ProjectAssignmentId,
                        principalTable: "ProjectAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Custodies_DepartmentId",
                table: "Custodies",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Custodies_HolderId",
                table: "Custodies",
                column: "HolderId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignProposals_PreparedById",
                table: "DesignProposals",
                column: "PreparedById");

            migrationBuilder.CreateIndex(
                name: "IX_DesignProposals_ProjectId",
                table: "DesignProposals",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRevisions_DocumentId",
                table: "DocumentRevisions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRevisions_PreparedById",
                table: "DocumentRevisions",
                column: "PreparedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialClaims_CreatedById",
                table: "FinancialClaims",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialClaims_ProjectId",
                table: "FinancialClaims",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_AssistantEngineerId",
                table: "ProjectAssignments",
                column: "AssistantEngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_LeadEngineerId",
                table: "ProjectAssignments",
                column: "LeadEngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_ProjectId",
                table: "ProjectAssignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_StageId",
                table: "ProjectAssignments",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignmentSubtasks_ProjectAssignmentId",
                table: "ProjectAssignmentSubtasks",
                column: "ProjectAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StageTaskTemplates_ProjectStageId",
                table: "StageTaskTemplates",
                column: "ProjectStageId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalRequests_RequestedById",
                table: "TechnicalRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalRequests_RoutedToDepartmentId",
                table: "TechnicalRequests",
                column: "RoutedToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalRequests_SiteId",
                table: "TechnicalRequests",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalRequests_StageId",
                table: "TechnicalRequests",
                column: "StageId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialRecords_ProjectAssignments_ProjectAssignmentId",
                table: "FinancialRecords",
                column: "ProjectAssignmentId",
                principalTable: "ProjectAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialRecords_ProjectAssignments_ProjectAssignmentId",
                table: "FinancialRecords");

            migrationBuilder.DropTable(
                name: "Custodies");

            migrationBuilder.DropTable(
                name: "DesignProposals");

            migrationBuilder.DropTable(
                name: "DocumentRevisions");

            migrationBuilder.DropTable(
                name: "FinancialClaims");

            migrationBuilder.DropTable(
                name: "ProjectAssignmentSubtasks");

            migrationBuilder.DropTable(
                name: "StageTaskTemplates");

            migrationBuilder.DropTable(
                name: "TechnicalRequests");

            migrationBuilder.DropTable(
                name: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "ContractImagePath",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SuspendedReason",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLead",
                table: "TaskAssignees");

            migrationBuilder.DropColumn(
                name: "ActualDays",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ExpectedDays",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "OutputType",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ActualDeliveryDate",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "PricePerMeter",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "ActualDeliveryDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "NextOfKinPhone",
                schema: "identity",
                table: "Users",
                newName: "PersonalId");

            migrationBuilder.RenameColumn(
                name: "ProjectAssignmentId",
                table: "FinancialRecords",
                newName: "ProjectCostId");

            migrationBuilder.RenameIndex(
                name: "IX_FinancialRecords_ProjectAssignmentId",
                table: "FinancialRecords",
                newName: "IX_FinancialRecords_ProjectCostId");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectCosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Area = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CostType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DiscountOrAdditionPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsTransferredToFinance = table.Column<bool>(type: "boolean", nullable: false),
                    PricePerMeter = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransferredToFinanceAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCosts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCostSubtasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectCostId = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCostSubtasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCostSubtasks_ProjectCosts_ProjectCostId",
                        column: x => x.ProjectCostId,
                        principalTable: "ProjectCosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonalId",
                schema: "identity",
                table: "Users",
                column: "PersonalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCosts_ProjectId",
                table: "ProjectCosts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCostSubtasks_ProjectCostId",
                table: "ProjectCostSubtasks",
                column: "ProjectCostId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialRecords_ProjectCosts_ProjectCostId",
                table: "FinancialRecords",
                column: "ProjectCostId",
                principalTable: "ProjectCosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
