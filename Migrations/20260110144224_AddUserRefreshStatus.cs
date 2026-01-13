using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AurionCal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRefreshStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRefreshStatuses",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsecutiveFailureCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAttemptUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSuccessUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NextAttemptUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureEmailSentUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshStatuses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserRefreshStatuses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRefreshStatuses");
        }
    }
}
