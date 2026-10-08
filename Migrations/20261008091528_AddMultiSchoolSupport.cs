using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AurionCal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSchoolSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_JuniaEmail",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "JuniaEmail",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "JuniaPassword",
                table: "Users",
                newName: "Password");

            // Emails are now stored trimmed and lowercase. This fails (and rolls back) if two
            // existing accounts only differ by case, which then has to be resolved manually.
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Email\" = lower(trim(\"Email\"));");

            // All existing accounts are Junia accounts
            migrationBuilder.AddColumn<string>(
                name: "SchoolId",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "junia");

            migrationBuilder.AlterColumn<string>(
                name: "SchoolId",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "junia");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Users",
                newName: "JuniaPassword");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Users",
                newName: "JuniaEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_JuniaEmail",
                table: "Users",
                column: "JuniaEmail",
                unique: true);
        }
    }
}
