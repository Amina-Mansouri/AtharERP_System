using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class updateentity3081 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                schema: "identity",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "identity",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "identity",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "identity",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                schema: "identity",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
