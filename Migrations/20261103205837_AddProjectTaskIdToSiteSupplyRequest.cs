using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTaskIdToSiteSupplyRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectTaskId",
                table: "SiteSupplyRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSupplyRequests_ProjectTaskId",
                table: "SiteSupplyRequests",
                column: "ProjectTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSupplyRequests_ProjectTasks_ProjectTaskId",
                table: "SiteSupplyRequests",
                column: "ProjectTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteSupplyRequests_ProjectTasks_ProjectTaskId",
                table: "SiteSupplyRequests");

            migrationBuilder.DropIndex(
                name: "IX_SiteSupplyRequests_ProjectTaskId",
                table: "SiteSupplyRequests");

            migrationBuilder.DropColumn(
                name: "ProjectTaskId",
                table: "SiteSupplyRequests");
        }
    }
}
