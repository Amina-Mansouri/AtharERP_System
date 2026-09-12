using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAssignmentFinalAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "ProjectAssignments");

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedValue",
                table: "ProjectTasks",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "ProjectTasks",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "ProjectTasks");

            migrationBuilder.AlterColumn<decimal>(
                name: "EstimatedValue",
                table: "ProjectTasks",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "ProjectAssignments",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
