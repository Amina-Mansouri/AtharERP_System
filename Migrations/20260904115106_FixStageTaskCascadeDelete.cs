using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class FixStageTaskCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectStages_StageId",
                table: "ProjectTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectStages_StageId",
                table: "ProjectTasks",
                column: "StageId",
                principalTable: "ProjectStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectStages_StageId",
                table: "ProjectTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectStages_StageId",
                table: "ProjectTasks",
                column: "StageId",
                principalTable: "ProjectStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
