using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class updateEntity3085 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectAssignmentId",
                table: "ProjectTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectAssignmentId",
                table: "ProjectTasks",
                column: "ProjectAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectAssignments_ProjectAssignmentId",
                table: "ProjectTasks",
                column: "ProjectAssignmentId",
                principalTable: "ProjectAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectAssignments_ProjectAssignmentId",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ProjectAssignmentId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ProjectAssignmentId",
                table: "ProjectTasks");
        }
    }
}
