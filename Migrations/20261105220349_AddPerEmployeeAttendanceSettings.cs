using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddPerEmployeeAttendanceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceMethodType",
                schema: "identity",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ExpectedEndTime",
                schema: "identity",
                table: "Users",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ExpectedStartTime",
                schema: "identity",
                table: "Users",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceMethodType",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExpectedEndTime",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExpectedStartTime",
                schema: "identity",
                table: "Users");
        }
    }
}
