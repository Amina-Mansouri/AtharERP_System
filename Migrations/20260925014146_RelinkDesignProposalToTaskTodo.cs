using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtharERP_System.Migrations
{
    /// <inheritdoc />
    public partial class RelinkDesignProposalToTaskTodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignProposals_ProjectTasks_ProjectTaskId",
                table: "DesignProposals");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectTaskId",
                table: "DesignProposals",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "TaskTodoId",
                table: "DesignProposals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DesignProposals_TaskTodoId",
                table: "DesignProposals",
                column: "TaskTodoId");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignProposals_ProjectTasks_ProjectTaskId",
                table: "DesignProposals",
                column: "ProjectTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignProposals_TaskTodos_TaskTodoId",
                table: "DesignProposals",
                column: "TaskTodoId",
                principalTable: "TaskTodos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignProposals_ProjectTasks_ProjectTaskId",
                table: "DesignProposals");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignProposals_TaskTodos_TaskTodoId",
                table: "DesignProposals");

            migrationBuilder.DropIndex(
                name: "IX_DesignProposals_TaskTodoId",
                table: "DesignProposals");

            migrationBuilder.DropColumn(
                name: "TaskTodoId",
                table: "DesignProposals");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectTaskId",
                table: "DesignProposals",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignProposals_ProjectTasks_ProjectTaskId",
                table: "DesignProposals",
                column: "ProjectTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
