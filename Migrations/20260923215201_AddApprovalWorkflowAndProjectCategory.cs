using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflowAndProjectCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientReply",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "ManagerComment",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "DesignProposals");

            migrationBuilder.AddColumn<string>(
                name: "SignatureImagePath",
                schema: "identity",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectCategoryId",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StampImagePath",
                table: "Departments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Classification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProposalReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectTaskId = table.Column<int>(type: "integer", nullable: true),
                    DesignProposalId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ReviewerPosition = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReviewerSignaturePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SupervisorName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SupervisorPosition = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SupervisorSignaturePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Discipline = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PdfFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedById = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalReviews_DesignProposals_DesignProposalId",
                        column: x => x.DesignProposalId,
                        principalTable: "DesignProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalReviews_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalReviews_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectCategoryId",
                table: "Projects",
                column: "ProjectCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalReviews_DesignProposalId",
                table: "ProposalReviews",
                column: "DesignProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalReviews_ProjectTaskId",
                table: "ProposalReviews",
                column: "ProjectTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalReviews_ReviewedById",
                table: "ProposalReviews",
                column: "ReviewedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectCategories_ProjectCategoryId",
                table: "Projects",
                column: "ProjectCategoryId",
                principalTable: "ProjectCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectCategories_ProjectCategoryId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectCategories");

            migrationBuilder.DropTable(
                name: "ProposalReviews");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectCategoryId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SignatureImagePath",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProjectCategoryId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "StampImagePath",
                table: "Departments");

            migrationBuilder.AddColumn<string>(
                name: "ClientReply",
                table: "DesignProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerComment",
                table: "DesignProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "DesignProposals",
                type: "text",
                nullable: true);
        }
    }
}
