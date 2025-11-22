using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseholdManager.Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AllowUserDeletionWithTaskHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdMembers_Users_UserId",
                table: "HouseholdMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdTasks_Users_AssignedUserId",
                table: "HouseholdTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdMembers_Users_UserId",
                table: "HouseholdMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdTasks_Users_AssignedUserId",
                table: "HouseholdTasks",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdMembers_Users_UserId",
                table: "HouseholdMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdTasks_Users_AssignedUserId",
                table: "HouseholdTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdMembers_Users_UserId",
                table: "HouseholdMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdTasks_Users_AssignedUserId",
                table: "HouseholdTasks",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
