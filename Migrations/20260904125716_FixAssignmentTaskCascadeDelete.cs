using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class FixAssignmentTaskCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectAssignments_ProjectAssignmentId",
                table: "ProjectTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectAssignments_ProjectAssignmentId",
                table: "ProjectTasks",
                column: "ProjectAssignmentId",
                principalTable: "ProjectAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectAssignments_ProjectAssignmentId",
                table: "ProjectTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectAssignments_ProjectAssignmentId",
                table: "ProjectTasks",
                column: "ProjectAssignmentId",
                principalTable: "ProjectAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
