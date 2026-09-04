using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class updateEntity3086 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StageTemplateTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StageTemplateId = table.Column<int>(type: "integer", nullable: false),
                    TaskName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageTemplateTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageTemplateTasks_StageTemplates_StageTemplateId",
                        column: x => x.StageTemplateId,
                        principalTable: "StageTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StageTemplateTasks_StageTemplateId",
                table: "StageTemplateTasks",
                column: "StageTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StageTemplateTasks");

            migrationBuilder.DropTable(
                name: "StageTemplates");
        }
    }
}
