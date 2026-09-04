using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentMultipleEngineers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignments_Users_AssistantEngineerId",
                table: "ProjectAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignments_Users_LeadEngineerId",
                table: "ProjectAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAssignments_AssistantEngineerId",
                table: "ProjectAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAssignments_LeadEngineerId",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "AssistantEngineerId",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "LeadEngineerId",
                table: "ProjectAssignments");

            migrationBuilder.CreateTable(
                name: "AssignmentEngineers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentEngineers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentEngineers_ProjectAssignments_ProjectAssignmentId",
                        column: x => x.ProjectAssignmentId,
                        principalTable: "ProjectAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentEngineers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentEngineers_ProjectAssignmentId",
                table: "AssignmentEngineers",
                column: "ProjectAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentEngineers_UserId",
                table: "AssignmentEngineers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentEngineers");

            migrationBuilder.AddColumn<string>(
                name: "AssistantEngineerId",
                table: "ProjectAssignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadEngineerId",
                table: "ProjectAssignments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_AssistantEngineerId",
                table: "ProjectAssignments",
                column: "AssistantEngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_LeadEngineerId",
                table: "ProjectAssignments",
                column: "LeadEngineerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignments_Users_AssistantEngineerId",
                table: "ProjectAssignments",
                column: "AssistantEngineerId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignments_Users_LeadEngineerId",
                table: "ProjectAssignments",
                column: "LeadEngineerId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
