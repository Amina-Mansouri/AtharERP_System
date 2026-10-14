using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSiteRequirementWithCorrespondence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteRequirements");

            migrationBuilder.CreateTable(
                name: "SiteCorrespondences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    RequestNumber = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RequiredResponseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DetailedQuery = table.Column<string>(type: "text", nullable: false),
                    AttachmentPath = table.Column<string>(type: "text", nullable: true),
                    AttachmentFileName = table.Column<string>(type: "text", nullable: true),
                    IsFromContractor = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedByName = table.Column<string>(type: "text", nullable: false),
                    SubmittedByPosition = table.Column<string>(type: "text", nullable: true),
                    SubmittedBySignaturePath = table.Column<string>(type: "text", nullable: true),
                    ApproverName = table.Column<string>(type: "text", nullable: true),
                    ApproverPosition = table.Column<string>(type: "text", nullable: true),
                    ApproverSignaturePath = table.Column<string>(type: "text", nullable: true),
                    SentById = table.Column<string>(type: "text", nullable: true),
                    SentByContractorId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ParentCorrespondenceId = table.Column<int>(type: "integer", nullable: true),
                    PdfFilePath = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteCorrespondences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteCorrespondences_Contractors_SentByContractorId",
                        column: x => x.SentByContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteCorrespondences_SiteCorrespondences_ParentCorrespondenc~",
                        column: x => x.ParentCorrespondenceId,
                        principalTable: "SiteCorrespondences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteCorrespondences_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SiteCorrespondences_Users_SentById",
                        column: x => x.SentById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteCorrespondences_ParentCorrespondenceId",
                table: "SiteCorrespondences",
                column: "ParentCorrespondenceId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteCorrespondences_SentByContractorId",
                table: "SiteCorrespondences",
                column: "SentByContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteCorrespondences_SentById",
                table: "SiteCorrespondences",
                column: "SentById");

            migrationBuilder.CreateIndex(
                name: "IX_SiteCorrespondences_SiteId",
                table: "SiteCorrespondences",
                column: "SiteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteCorrespondences");

            migrationBuilder.CreateTable(
                name: "SiteRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SentByContractorId = table.Column<int>(type: "integer", nullable: true),
                    SentById = table.Column<string>(type: "text", nullable: true),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "text", nullable: true),
                    AttachmentPath = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFromContractor = table.Column<bool>(type: "boolean", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteRequirements_Contractors_SentByContractorId",
                        column: x => x.SentByContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteRequirements_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SiteRequirements_Users_SentById",
                        column: x => x.SentById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteRequirements_SentByContractorId",
                table: "SiteRequirements",
                column: "SentByContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteRequirements_SentById",
                table: "SiteRequirements",
                column: "SentById");

            migrationBuilder.CreateIndex(
                name: "IX_SiteRequirements_SiteId",
                table: "SiteRequirements",
                column: "SiteId");
        }
    }
}
