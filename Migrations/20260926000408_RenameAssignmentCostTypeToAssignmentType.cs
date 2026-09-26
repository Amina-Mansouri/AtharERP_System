using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RenameAssignmentCostTypeToAssignmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CostType",
                table: "ProjectAssignments",
                newName: "AssignmentType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AssignmentType",
                table: "ProjectAssignments",
                newName: "CostType");
        }
    }
}
