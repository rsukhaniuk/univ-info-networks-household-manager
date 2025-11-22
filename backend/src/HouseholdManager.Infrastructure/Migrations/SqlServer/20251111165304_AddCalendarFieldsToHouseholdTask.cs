using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseholdManager.Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddCalendarFieldsToHouseholdTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalCalendarId",
                table: "HouseholdTasks",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "HouseholdTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndDate",
                table: "HouseholdTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceRule",
                table: "HouseholdTasks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalCalendarId",
                table: "HouseholdTasks");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "HouseholdTasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "HouseholdTasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceRule",
                table: "HouseholdTasks");
        }
    }
}
