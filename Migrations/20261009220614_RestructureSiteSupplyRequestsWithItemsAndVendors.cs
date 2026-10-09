using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RestructureSiteSupplyRequestsWithItemsAndVendors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteSupplyRequests_Contractors_RequestedByContractorId",
                table: "SiteSupplyRequests");

            migrationBuilder.DropColumn(
                name: "Dimensions",
                table: "SiteSupplyRequests");

            migrationBuilder.DropColumn(
                name: "MaterialName",
                table: "SiteSupplyRequests");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "SiteSupplyRequests");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "SiteSupplyRequests");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SiteSupplyRequests");

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSupplyRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteSupplyRequestId = table.Column<int>(type: "integer", nullable: false),
                    MaterialName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhotoPath = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VendorId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSupplyRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteSupplyRequestItems_SiteSupplyRequests_SiteSupplyRequest~",
                        column: x => x.SiteSupplyRequestId,
                        principalTable: "SiteSupplyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SiteSupplyRequestItems_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteSupplyRequestItems_SiteSupplyRequestId",
                table: "SiteSupplyRequestItems",
                column: "SiteSupplyRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSupplyRequestItems_VendorId",
                table: "SiteSupplyRequestItems",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSupplyRequests_Contractors_RequestedByContractorId",
                table: "SiteSupplyRequests",
                column: "RequestedByContractorId",
                principalTable: "Contractors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteSupplyRequests_Contractors_RequestedByContractorId",
                table: "SiteSupplyRequests");

            migrationBuilder.DropTable(
                name: "SiteSupplyRequestItems");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.AddColumn<string>(
                name: "Dimensions",
                table: "SiteSupplyRequests",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialName",
                table: "SiteSupplyRequests",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "SiteSupplyRequests",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "SiteSupplyRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SiteSupplyRequests",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSupplyRequests_Contractors_RequestedByContractorId",
                table: "SiteSupplyRequests",
                column: "RequestedByContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
