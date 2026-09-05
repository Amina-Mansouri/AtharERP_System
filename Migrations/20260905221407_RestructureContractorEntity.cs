using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RestructureContractorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "SiteContractors");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SiteContractors");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "SiteContractors");

            migrationBuilder.AddColumn<int>(
                name: "ContractorId",
                table: "SiteContractors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Contractors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contractors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteContractors_ContractorId",
                table: "SiteContractors",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contractors_Email",
                table: "Contractors",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteContractors_Contractors_ContractorId",
                table: "SiteContractors",
                column: "ContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteContractors_Contractors_ContractorId",
                table: "SiteContractors");

            migrationBuilder.DropTable(
                name: "Contractors");

            migrationBuilder.DropIndex(
                name: "IX_SiteContractors_ContractorId",
                table: "SiteContractors");

            migrationBuilder.DropColumn(
                name: "ContractorId",
                table: "SiteContractors");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "SiteContractors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SiteContractors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "SiteContractors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
