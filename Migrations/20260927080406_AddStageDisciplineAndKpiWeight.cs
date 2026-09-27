using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddStageDisciplineAndKpiWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Discipline",
                table: "ProjectStages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KpiWeight",
                table: "ProjectStages",
                type: "numeric(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discipline",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "KpiWeight",
                table: "ProjectStages");
        }
    }
}
