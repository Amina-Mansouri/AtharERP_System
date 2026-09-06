using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSiteOperationResponsible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteOperations_Users_ResponsibleId",
                table: "SiteOperations");

            migrationBuilder.DropIndex(
                name: "IX_SiteOperations_ResponsibleId",
                table: "SiteOperations");

            migrationBuilder.DropColumn(
                name: "ResponsibleId",
                table: "SiteOperations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponsibleId",
                table: "SiteOperations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteOperations_ResponsibleId",
                table: "SiteOperations",
                column: "ResponsibleId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteOperations_Users_ResponsibleId",
                table: "SiteOperations",
                column: "ResponsibleId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
