using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddCustodyRebuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Custodies");

            migrationBuilder.CreateTable(
                name: "CustodyCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustodyHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HolderId = table.Column<string>(type: "text", nullable: false),
                    HandedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: false),
                    HolderConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    HolderConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustodyHandovers_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyHandovers_Users_HolderId",
                        column: x => x.HolderId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustodyItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustodyItems_CustodyCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CustodyCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustodyHandoverLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustodyHandoverId = table.Column<int>(type: "integer", nullable: false),
                    CustodyItemId = table.Column<int>(type: "integer", nullable: false),
                    QuantityHanded = table.Column<int>(type: "integer", nullable: false),
                    QuantityReturned = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyHandoverLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustodyHandoverLines_CustodyHandovers_CustodyHandoverId",
                        column: x => x.CustodyHandoverId,
                        principalTable: "CustodyHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustodyHandoverLines_CustodyItems_CustodyItemId",
                        column: x => x.CustodyItemId,
                        principalTable: "CustodyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyHandoverLines_CustodyHandoverId",
                table: "CustodyHandoverLines",
                column: "CustodyHandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyHandoverLines_CustodyItemId",
                table: "CustodyHandoverLines",
                column: "CustodyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyHandovers_CreatedById",
                table: "CustodyHandovers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyHandovers_HolderId",
                table: "CustodyHandovers",
                column: "HolderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyItems_CategoryId",
                table: "CustodyItems",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustodyHandoverLines");

            migrationBuilder.DropTable(
                name: "CustodyHandovers");

            migrationBuilder.DropTable(
                name: "CustodyItems");

            migrationBuilder.DropTable(
                name: "CustodyCategories");

            migrationBuilder.CreateTable(
                name: "Custodies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DepartmentId = table.Column<int>(type: "integer", nullable: true),
                    HolderId = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HandedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ReturnedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Custodies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Custodies_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Custodies_Users_HolderId",
                        column: x => x.HolderId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Custodies_DepartmentId",
                table: "Custodies",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Custodies_HolderId",
                table: "Custodies",
                column: "HolderId");
        }
    }
}
