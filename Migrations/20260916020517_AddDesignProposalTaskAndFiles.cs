using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignProposalTaskAndFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "DesignProposals",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "DesignProposals",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "DesignProposals",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "DesignProposals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerComment",
                table: "DesignProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectTaskId",
                table: "DesignProposals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DesignProposals_ProjectTaskId",
                table: "DesignProposals",
                column: "ProjectTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignProposals_ProjectTasks_ProjectTaskId",
                table: "DesignProposals",
                column: "ProjectTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignProposals_ProjectTasks_ProjectTaskId",
                table: "DesignProposals");

            migrationBuilder.DropIndex(
                name: "IX_DesignProposals_ProjectTaskId",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "ManagerComment",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "ProjectTaskId",
                table: "DesignProposals");
        }
    }
}
