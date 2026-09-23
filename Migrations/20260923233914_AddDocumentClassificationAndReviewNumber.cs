using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentClassificationAndReviewNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewNumber",
                table: "ProposalReviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "DesignProposals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FileCategory",
                table: "DesignProposals",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewNumber",
                table: "ProposalReviews");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "FileCategory",
                table: "DesignProposals");
        }
    }
}
