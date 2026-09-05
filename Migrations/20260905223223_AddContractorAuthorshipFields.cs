using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorAuthorshipFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RequestedById",
                table: "SiteSupplyRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "RequestedByContractorId",
                table: "SiteSupplyRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CheckedById",
                table: "SiteSafetyChecks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "CheckedByContractorId",
                table: "SiteSafetyChecks",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CheckedById",
                table: "SiteQualityChecks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "CheckedByContractorId",
                table: "SiteQualityChecks",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "SiteDailyReports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByContractorId",
                table: "SiteDailyReports",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSupplyRequests_RequestedByContractorId",
                table: "SiteSupplyRequests",
                column: "RequestedByContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSafetyChecks_CheckedByContractorId",
                table: "SiteSafetyChecks",
                column: "CheckedByContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteQualityChecks_CheckedByContractorId",
                table: "SiteQualityChecks",
                column: "CheckedByContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteDailyReports_CreatedByContractorId",
                table: "SiteDailyReports",
                column: "CreatedByContractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteDailyReports_Contractors_CreatedByContractorId",
                table: "SiteDailyReports",
                column: "CreatedByContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteQualityChecks_Contractors_CheckedByContractorId",
                table: "SiteQualityChecks",
                column: "CheckedByContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSafetyChecks_Contractors_CheckedByContractorId",
                table: "SiteSafetyChecks",
                column: "CheckedByContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSupplyRequests_Contractors_RequestedByContractorId",
                table: "SiteSupplyRequests",
                column: "RequestedByContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteDailyReports_Contractors_CreatedByContractorId",
                table: "SiteDailyReports");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteQualityChecks_Contractors_CheckedByContractorId",
                table: "SiteQualityChecks");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSafetyChecks_Contractors_CheckedByContractorId",
                table: "SiteSafetyChecks");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSupplyRequests_Contractors_RequestedByContractorId",
                table: "SiteSupplyRequests");

            migrationBuilder.DropIndex(
                name: "IX_SiteSupplyRequests_RequestedByContractorId",
                table: "SiteSupplyRequests");

            migrationBuilder.DropIndex(
                name: "IX_SiteSafetyChecks_CheckedByContractorId",
                table: "SiteSafetyChecks");

            migrationBuilder.DropIndex(
                name: "IX_SiteQualityChecks_CheckedByContractorId",
                table: "SiteQualityChecks");

            migrationBuilder.DropIndex(
                name: "IX_SiteDailyReports_CreatedByContractorId",
                table: "SiteDailyReports");

            migrationBuilder.DropColumn(
                name: "RequestedByContractorId",
                table: "SiteSupplyRequests");

            migrationBuilder.DropColumn(
                name: "CheckedByContractorId",
                table: "SiteSafetyChecks");

            migrationBuilder.DropColumn(
                name: "CheckedByContractorId",
                table: "SiteQualityChecks");

            migrationBuilder.DropColumn(
                name: "CreatedByContractorId",
                table: "SiteDailyReports");

            migrationBuilder.AlterColumn<string>(
                name: "RequestedById",
                table: "SiteSupplyRequests",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CheckedById",
                table: "SiteSafetyChecks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CheckedById",
                table: "SiteQualityChecks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "SiteDailyReports",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
