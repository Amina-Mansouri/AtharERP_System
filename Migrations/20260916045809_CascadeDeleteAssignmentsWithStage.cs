using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteAssignmentsWithStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignments_ProjectStages_StageId",
                table: "ProjectAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignments_ProjectStages_StageId",
                table: "ProjectAssignments",
                column: "StageId",
                principalTable: "ProjectStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignments_ProjectStages_StageId",
                table: "ProjectAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignments_ProjectStages_StageId",
                table: "ProjectAssignments",
                column: "StageId",
                principalTable: "ProjectStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
