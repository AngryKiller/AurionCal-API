using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AurionCal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAccommodations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExamAccommodations",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExamAccommodations",
                table: "Users");
        }
    }
}
