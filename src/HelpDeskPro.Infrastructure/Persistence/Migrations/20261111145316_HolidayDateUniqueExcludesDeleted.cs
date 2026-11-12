using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HolidayDateUniqueExcludesDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Holidays_Date",
                table: "Holidays");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date",
                table: "Holidays",
                column: "Date",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Holidays_Date",
                table: "Holidays");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date",
                table: "Holidays",
                column: "Date",
                unique: true);
        }
    }
}
