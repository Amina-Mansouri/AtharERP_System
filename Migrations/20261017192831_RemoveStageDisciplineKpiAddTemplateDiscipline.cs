using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStageDisciplineKpiAddTemplateDiscipline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectStages_Departments_DisciplineDepartmentId",
                table: "ProjectStages");

            migrationBuilder.DropIndex(
                name: "IX_ProjectStages_DisciplineDepartmentId",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "DisciplineDepartmentId",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "KpiWeight",
                table: "ProjectStages");

            migrationBuilder.AddColumn<int>(
                name: "DisciplineDepartmentId",
                table: "StageTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStageDisciplinesParent",
                table: "Departments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StageTemplates_DisciplineDepartmentId",
                table: "StageTemplates",
                column: "DisciplineDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StageTemplates_Departments_DisciplineDepartmentId",
                table: "StageTemplates",
                column: "DisciplineDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StageTemplates_Departments_DisciplineDepartmentId",
                table: "StageTemplates");

            migrationBuilder.DropIndex(
                name: "IX_StageTemplates_DisciplineDepartmentId",
                table: "StageTemplates");

            migrationBuilder.DropColumn(
                name: "DisciplineDepartmentId",
                table: "StageTemplates");

            migrationBuilder.DropColumn(
                name: "IsStageDisciplinesParent",
                table: "Departments");

            migrationBuilder.AddColumn<int>(
                name: "DisciplineDepartmentId",
                table: "ProjectStages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KpiWeight",
                table: "ProjectStages",
                type: "numeric(5,2)",
                nullable: true);

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
    }
}
