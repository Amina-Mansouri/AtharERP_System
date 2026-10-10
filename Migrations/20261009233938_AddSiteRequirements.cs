using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    AttachmentPath = table.Column<string>(type: "text", nullable: true),
                    AttachmentFileName = table.Column<string>(type: "text", nullable: true),
                    IsFromContractor = table.Column<bool>(type: "boolean", nullable: false),
                    SentById = table.Column<string>(type: "text", nullable: true),
                    SentByContractorId = table.Column<int>(type: "integer", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteRequirements");
        }
    }
}
