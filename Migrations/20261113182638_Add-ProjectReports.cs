using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectClosureReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    ReportCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<string>(type: "text", nullable: true),
                    ClosureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScopeFullyCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ScopePartiallyWithNotes = table.Column<bool>(type: "boolean", nullable: false),
                    ScopeExcludedWorkExists = table.Column<bool>(type: "boolean", nullable: false),
                    ScopeNotes = table.Column<string>(type: "text", nullable: true),
                    FinalStatus = table.Column<int>(type: "integer", nullable: true),
                    FinalStatusNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedById = table.Column<string>(type: "text", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PdfFilePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectClosureReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectClosureReports_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectClosureReports_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectClosureReports_Users_DecidedById",
                        column: x => x.DecidedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectDeliveryReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    ProjectStageId = table.Column<int>(type: "integer", nullable: false),
                    ReportCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssignmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedById = table.Column<string>(type: "text", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PdfFilePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDeliveryReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDeliveryReports_ProjectStages_ProjectStageId",
                        column: x => x.ProjectStageId,
                        principalTable: "ProjectStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDeliveryReports_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDeliveryReports_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDeliveryReports_Users_DecidedById",
                        column: x => x.DecidedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectClosureReportStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectClosureReportId = table.Column<int>(type: "integer", nullable: false),
                    ProjectStageId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectClosureReportStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectClosureReportStages_ProjectClosureReports_ProjectClo~",
                        column: x => x.ProjectClosureReportId,
                        principalTable: "ProjectClosureReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectClosureReportStages_ProjectStages_ProjectStageId",
                        column: x => x.ProjectStageId,
                        principalTable: "ProjectStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDeliveryReportFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectDeliveryReportId = table.Column<int>(type: "integer", nullable: false),
                    DocumentRevisionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDeliveryReportFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDeliveryReportFiles_DocumentRevisions_DocumentRevisi~",
                        column: x => x.DocumentRevisionId,
                        principalTable: "DocumentRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDeliveryReportFiles_ProjectDeliveryReports_ProjectDe~",
                        column: x => x.ProjectDeliveryReportId,
                        principalTable: "ProjectDeliveryReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClosureReports_CreatedById",
                table: "ProjectClosureReports",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClosureReports_DecidedById",
                table: "ProjectClosureReports",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClosureReports_ProjectId",
                table: "ProjectClosureReports",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClosureReportStages_ProjectClosureReportId",
                table: "ProjectClosureReportStages",
                column: "ProjectClosureReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClosureReportStages_ProjectStageId",
                table: "ProjectClosureReportStages",
                column: "ProjectStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliveryReportFiles_DocumentRevisionId",
                table: "ProjectDeliveryReportFiles",
                column: "DocumentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliveryReportFiles_ProjectDeliveryReportId",
                table: "ProjectDeliveryReportFiles",
                column: "ProjectDeliveryReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliveryReports_CreatedById",
                table: "ProjectDeliveryReports",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliveryReports_DecidedById",
                table: "ProjectDeliveryReports",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliveryReports_ProjectId",
                table: "ProjectDeliveryReports",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliveryReports_ProjectStageId",
                table: "ProjectDeliveryReports",
                column: "ProjectStageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectClosureReportStages");

            migrationBuilder.DropTable(
                name: "ProjectDeliveryReportFiles");

            migrationBuilder.DropTable(
                name: "ProjectClosureReports");

            migrationBuilder.DropTable(
                name: "ProjectDeliveryReports");
        }
    }
}
