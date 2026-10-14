using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteCorrespondenceAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "SiteCorrespondences");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "SiteCorrespondences");

            migrationBuilder.CreateTable(
                name: "SiteCorrespondenceAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteCorrespondenceId = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteCorrespondenceAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteCorrespondenceAttachments_SiteCorrespondences_SiteCorre~",
                        column: x => x.SiteCorrespondenceId,
                        principalTable: "SiteCorrespondences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteCorrespondenceAttachments_SiteCorrespondenceId",
                table: "SiteCorrespondenceAttachments",
                column: "SiteCorrespondenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteCorrespondenceAttachments");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "SiteCorrespondences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "SiteCorrespondences",
                type: "text",
                nullable: true);
        }
    }
}
