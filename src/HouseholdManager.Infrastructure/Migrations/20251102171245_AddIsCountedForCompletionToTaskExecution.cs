using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseholdManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCountedForCompletionToTaskExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCountedForCompletion",
                table: "TaskExecutions",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCountedForCompletion",
                table: "TaskExecutions");
        }
    }
}
