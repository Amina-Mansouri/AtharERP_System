using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddRedesignOfAssignmentIdToProjectAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RedesignOfAssignmentId",
                table: "ProjectAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignments_RedesignOfAssignmentId",
                table: "ProjectAssignments",
                column: "RedesignOfAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignments_ProjectAssignments_RedesignOfAssignmentId",
                table: "ProjectAssignments",
                column: "RedesignOfAssignmentId",
                principalTable: "ProjectAssignments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignments_ProjectAssignments_RedesignOfAssignmentId",
                table: "ProjectAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAssignments_RedesignOfAssignmentId",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "RedesignOfAssignmentId",
                table: "ProjectAssignments");
        }
    }
}
