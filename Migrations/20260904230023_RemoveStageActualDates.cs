using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStageActualDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualEndDate",
                table: "ProjectStages");

            migrationBuilder.DropColumn(
                name: "ActualStartDate",
                table: "ProjectStages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndDate",
                table: "ProjectStages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartDate",
                table: "ProjectStages",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
