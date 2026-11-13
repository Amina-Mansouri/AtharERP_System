using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddResignationTaskClearanceEffectiveDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedLastWorkingDay",
                table: "ResignationRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveLastWorkingDay",
                table: "ResignationRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResignationTaskClearances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResignationRequestId = table.Column<int>(type: "integer", nullable: false),
                    ProjectAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResignationTaskClearances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResignationTaskClearances_ProjectAssignments_ProjectAssignm~",
                        column: x => x.ProjectAssignmentId,
                        principalTable: "ProjectAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResignationTaskClearances_ResignationRequests_ResignationRe~",
                        column: x => x.ResignationRequestId,
                        principalTable: "ResignationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResignationTaskClearances_ProjectAssignmentId",
                table: "ResignationTaskClearances",
                column: "ProjectAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResignationTaskClearances_ResignationRequestId",
                table: "ResignationTaskClearances",
                column: "ResignationRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResignationTaskClearances");

            migrationBuilder.DropColumn(
                name: "EffectiveLastWorkingDay",
                table: "ResignationRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedLastWorkingDay",
                table: "ResignationRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
