using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateStageSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectStages_ProjectId_Sequence",
                table: "ProjectStages");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStages_ProjectId_Sequence",
                table: "ProjectStages",
                columns: new[] { "ProjectId", "Sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectStages_ProjectId_Sequence",
                table: "ProjectStages");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStages_ProjectId_Sequence",
                table: "ProjectStages",
                columns: new[] { "ProjectId", "Sequence" },
                unique: true);
        }
    }
}
