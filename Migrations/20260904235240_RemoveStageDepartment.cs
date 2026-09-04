using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStageDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectStages_Departments_DepartmentId",
                table: "ProjectStages");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStages_DepartmentId",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ProjectStages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ProjectStages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStages_DepartmentId",
                table: "ProjectStages",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectStages_Departments_DepartmentId",
                table: "ProjectStages",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
