using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class MoveStageDisciplineToDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Discipline",
                table: "ProjectStages",
                newName: "DisciplineDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStages_DisciplineDepartmentId",
                table: "ProjectStages",
                column: "DisciplineDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectStages_Departments_DisciplineDepartmentId",
                table: "ProjectStages",
                column: "DisciplineDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectStages_Departments_DisciplineDepartmentId",
                table: "ProjectStages");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStages_DisciplineDepartmentId",
                table: "ProjectStages");

            migrationBuilder.RenameColumn(
                name: "DisciplineDepartmentId",
                table: "ProjectStages",
                newName: "Discipline");
        }
    }
}
