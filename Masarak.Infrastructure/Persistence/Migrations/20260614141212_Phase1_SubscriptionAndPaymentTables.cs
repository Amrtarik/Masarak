using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masarak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_SubscriptionAndPaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscriptions_UserId",
                table: "subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "StudentLinkageCode",
                table: "users",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "subscriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Active");

            migrationBuilder.AddColumn<int>(
                name: "ActivatedByAdminId",
                table: "subscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationMethod",
                table: "subscriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "subscriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "subscriptions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "subscriptions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "plans",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "plans",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripeChargeId",
                table: "payments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "payments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "parent_student_links",
                columns: table => new
                {
                    ParentStudentLinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentUserId = table.Column<int>(type: "int", nullable: false),
                    StudentUserId = table.Column<int>(type: "int", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parent_student_links", x => x.ParentStudentLinkId);
                    table.ForeignKey(
                        name: "FK_parent_student_links_users_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parent_student_links_users_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_users_StudentLinkageCode",
                table: "users",
                column: "StudentLinkageCode",
                unique: true,
                filter: "[StudentLinkageCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_StripeSessionId",
                table: "subscriptions",
                column: "StripeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_Status",
                table: "subscriptions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_parent_student_links_StudentUserId",
                table: "parent_student_links",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "UX_parent_student_links_Parent_Student",
                table: "parent_student_links",
                columns: new[] { "ParentUserId", "StudentUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parent_student_links");

            migrationBuilder.DropIndex(
                name: "UX_users_StudentLinkageCode",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_StripeSessionId",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_UserId_Status",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "StudentLinkageCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ActivatedByAdminId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ActivationMethod",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "StripeChargeId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "payments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "subscriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId",
                table: "subscriptions",
                column: "UserId");
        }
    }
}
