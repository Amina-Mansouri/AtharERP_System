using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskBonusPenalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BonusAmount",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "PenaltyAmount",
                table: "ProjectTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BonusAmount",
                table: "ProjectTasks",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyAmount",
                table: "ProjectTasks",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
