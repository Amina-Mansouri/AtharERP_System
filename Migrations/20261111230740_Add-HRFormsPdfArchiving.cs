using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddHRFormsPdfArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "ResignationRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "PermitRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "LeaveRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "CustodyHandovers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "ResignationRequests");

            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "PermitRequests");

            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "CustodyHandovers");
        }
    }
}
