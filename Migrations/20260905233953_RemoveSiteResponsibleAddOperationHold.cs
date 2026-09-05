using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSiteResponsibleAddOperationHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sites_Users_ResponsibleId",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_ResponsibleId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ResponsibleId",
                table: "Sites");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponsibleId",
                table: "Sites",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_ResponsibleId",
                table: "Sites",
                column: "ResponsibleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_Users_ResponsibleId",
                table: "Sites",
                column: "ResponsibleId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
