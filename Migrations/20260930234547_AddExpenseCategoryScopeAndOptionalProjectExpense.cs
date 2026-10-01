using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategoryScopeAndOptionalProjectExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExpenses_Projects_ProjectId",
                table: "ProjectExpenses");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectExpenses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "ExpenseCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExpenses_Projects_ProjectId",
                table: "ProjectExpenses",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectExpenses_Projects_ProjectId",
                table: "ProjectExpenses");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "ExpenseCategories");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectExpenses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectExpenses_Projects_ProjectId",
                table: "ProjectExpenses",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
