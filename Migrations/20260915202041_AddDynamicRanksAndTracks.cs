using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicRanksAndTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareerTrack",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Rank",
                schema: "identity",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "JobRankId",
                schema: "identity",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CareerTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerTracks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobRanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CareerTrackId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobRanks_CareerTracks_CareerTrackId",
                        column: x => x.CareerTrackId,
                        principalTable: "CareerTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_JobRankId",
                schema: "identity",
                table: "Users",
                column: "JobRankId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRanks_CareerTrackId",
                table: "JobRanks",
                column: "CareerTrackId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_JobRanks_JobRankId",
                schema: "identity",
                table: "Users",
                column: "JobRankId",
                principalTable: "JobRanks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_JobRanks_JobRankId",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropTable(
                name: "JobRanks");

            migrationBuilder.DropTable(
                name: "CareerTracks");

            migrationBuilder.DropIndex(
                name: "IX_Users_JobRankId",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JobRankId",
                schema: "identity",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "CareerTrack",
                schema: "identity",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rank",
                schema: "identity",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
