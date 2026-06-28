using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masarak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "NotifType",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "notifications");

            migrationBuilder.RenameColumn(
                name: "SentAt",
                table: "notifications",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "ActionUrl",
                table: "notifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "notifications",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "notifications",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead_CreatedAt",
                table: "notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_Unread",
                table: "notifications",
                columns: new[] { "UserId", "IsRead" },
                filter: "[IsRead] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId_IsRead_CreatedAt",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId_Unread",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ActionUrl",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "notifications");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "notifications",
                newName: "SentAt");

            migrationBuilder.AddColumn<string>(
                name: "NotifType",
                table: "notifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReferenceId",
                table: "notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "notifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                table: "notifications",
                column: "UserId");
        }
    }
}
