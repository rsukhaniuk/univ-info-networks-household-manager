using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseholdManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScheduledWeekdayColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Convert existing ScheduledWeekday values to RecurrenceRule
            // Sunday=0, Monday=1, Tuesday=2, Wednesday=3, Thursday=4, Friday=5, Saturday=6
            migrationBuilder.Sql(@"
                UPDATE HouseholdTasks
                SET RecurrenceRule =
                    CASE ScheduledWeekday
                        WHEN '0' THEN 'FREQ=WEEKLY;BYDAY=SU'
                        WHEN '1' THEN 'FREQ=WEEKLY;BYDAY=MO'
                        WHEN '2' THEN 'FREQ=WEEKLY;BYDAY=TU'
                        WHEN '3' THEN 'FREQ=WEEKLY;BYDAY=WE'
                        WHEN '4' THEN 'FREQ=WEEKLY;BYDAY=TH'
                        WHEN '5' THEN 'FREQ=WEEKLY;BYDAY=FR'
                        WHEN '6' THEN 'FREQ=WEEKLY;BYDAY=SA'
                    END
                WHERE Type = 'Regular'
                  AND ScheduledWeekday IS NOT NULL
                  AND (RecurrenceRule IS NULL OR RecurrenceRule = '');
            ");

            // Step 2: Drop the ScheduledWeekday column
            migrationBuilder.DropColumn(
                name: "ScheduledWeekday",
                table: "HouseholdTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScheduledWeekday",
                table: "HouseholdTasks",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
