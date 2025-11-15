using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseholdManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarSubscriptionTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarSubscriptionTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarSubscriptionTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarSubscriptionTokens_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarSubscriptionTokens_HouseholdId_UserId",
                table: "CalendarSubscriptionTokens",
                columns: new[] { "HouseholdId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarSubscriptionTokens_Token",
                table: "CalendarSubscriptionTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarSubscriptionTokens");
        }
    }
}
