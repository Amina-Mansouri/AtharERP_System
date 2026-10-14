using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaterialItemId",
                table: "SiteSupplyRequestItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NameAr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MaterialCategoryId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialItems_MaterialCategories_MaterialCategoryId",
                        column: x => x.MaterialCategoryId,
                        principalTable: "MaterialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteSupplyRequestItems_MaterialItemId",
                table: "SiteSupplyRequestItems",
                column: "MaterialItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialItems_MaterialCategoryId",
                table: "MaterialItems",
                column: "MaterialCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSupplyRequestItems_MaterialItems_MaterialItemId",
                table: "SiteSupplyRequestItems",
                column: "MaterialItemId",
                principalTable: "MaterialItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteSupplyRequestItems_MaterialItems_MaterialItemId",
                table: "SiteSupplyRequestItems");

            migrationBuilder.DropTable(
                name: "MaterialItems");

            migrationBuilder.DropTable(
                name: "MaterialCategories");

            migrationBuilder.DropIndex(
                name: "IX_SiteSupplyRequestItems_MaterialItemId",
                table: "SiteSupplyRequestItems");

            migrationBuilder.DropColumn(
                name: "MaterialItemId",
                table: "SiteSupplyRequestItems");
        }
    }
}
