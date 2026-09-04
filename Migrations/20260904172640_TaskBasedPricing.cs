using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class TaskBasedPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cost",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "DiscountOrAdditionPercent",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "PricePerMeter",
                table: "ProjectAssignments");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedValue",
                table: "ProjectTasks",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedValue",
                table: "ProjectTasks");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "ProjectStages",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "ProjectAssignments",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Area",
                table: "ProjectAssignments",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountOrAdditionPercent",
                table: "ProjectAssignments",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMeter",
                table: "ProjectAssignments",
                type: "numeric(18,2)",
                nullable: true);
        }
    }
}
