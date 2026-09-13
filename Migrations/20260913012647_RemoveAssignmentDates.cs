using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAssignmentDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualDate",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "AgreedDate",
                table: "ProjectAssignments");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "ProjectAssignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualDate",
                table: "ProjectAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AgreedDate",
                table: "ProjectAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "ProjectAssignments",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
