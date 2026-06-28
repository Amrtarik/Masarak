using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masarak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_AI_Analytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_recommendations_students_StudentId",
                table: "ai_recommendations");

            migrationBuilder.DropIndex(
                name: "IX_ai_recommendations_StudentId",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "ActionUrl",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "IsDismissed",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "RecType",
                table: "ai_recommendations");

            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "ai_recommendations",
                newName: "StudentUserId");

            migrationBuilder.RenameColumn(
                name: "ReferenceType",
                table: "ai_recommendations",
                newName: "ProviderUsed");

            migrationBuilder.RenameColumn(
                name: "ReferenceId",
                table: "ai_recommendations",
                newName: "SubjectId");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "ai_recommendations",
                newName: "Payload");

            migrationBuilder.RenameColumn(
                name: "RecommendationId",
                table: "ai_recommendations",
                newName: "AiRecommendationId");

            migrationBuilder.AddColumn<int>(
                name: "CompletionTokensUsed",
                table: "ai_recommendations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "ai_recommendations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ai_recommendations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokensUsed",
                table: "ai_recommendations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ai_recommendations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ai_prompt_templates",
                columns: table => new
                {
                    AiPromptTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_prompt_templates", x => x.AiPromptTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "analytics_snapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScopeEntityId = table.Column<int>(type: "int", nullable: true),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_snapshots", x => x.SnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "performance_alerts",
                columns: table => new
                {
                    PerformanceAlertId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentUserId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    AlertType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TriggerValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Threshold = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance_alerts", x => x.PerformanceAlertId);
                    table.ForeignKey(
                        name: "FK_performance_alerts_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_performance_alerts_users_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_StudentUserId_SubjectId_Type_IsActive",
                table: "ai_recommendations",
                columns: new[] { "StudentUserId", "SubjectId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_SubjectId",
                table: "ai_recommendations",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_prompt_templates_Key",
                table: "ai_prompt_templates",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_analytics_snapshots_Scope_ScopeEntityId",
                table: "analytics_snapshots",
                columns: new[] { "Scope", "ScopeEntityId" },
                unique: true,
                filter: "[ScopeEntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_performance_alerts_StudentUserId_SubjectId_AlertType_IsResolved",
                table: "performance_alerts",
                columns: new[] { "StudentUserId", "SubjectId", "AlertType", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_performance_alerts_SubjectId",
                table: "performance_alerts",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_recommendations_subjects_SubjectId",
                table: "ai_recommendations",
                column: "SubjectId",
                principalTable: "subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ai_recommendations_users_StudentUserId",
                table: "ai_recommendations",
                column: "StudentUserId",
                principalTable: "users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_recommendations_subjects_SubjectId",
                table: "ai_recommendations");

            migrationBuilder.DropForeignKey(
                name: "FK_ai_recommendations_users_StudentUserId",
                table: "ai_recommendations");

            migrationBuilder.DropTable(
                name: "ai_prompt_templates");

            migrationBuilder.DropTable(
                name: "analytics_snapshots");

            migrationBuilder.DropTable(
                name: "performance_alerts");

            migrationBuilder.DropIndex(
                name: "IX_ai_recommendations_StudentUserId_SubjectId_Type_IsActive",
                table: "ai_recommendations");

            migrationBuilder.DropIndex(
                name: "IX_ai_recommendations_SubjectId",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "CompletionTokensUsed",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "PromptTokensUsed",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ai_recommendations");

            migrationBuilder.RenameColumn(
                name: "SubjectId",
                table: "ai_recommendations",
                newName: "ReferenceId");

            migrationBuilder.RenameColumn(
                name: "StudentUserId",
                table: "ai_recommendations",
                newName: "StudentId");

            migrationBuilder.RenameColumn(
                name: "ProviderUsed",
                table: "ai_recommendations",
                newName: "ReferenceType");

            migrationBuilder.RenameColumn(
                name: "Payload",
                table: "ai_recommendations",
                newName: "Reason");

            migrationBuilder.RenameColumn(
                name: "AiRecommendationId",
                table: "ai_recommendations",
                newName: "RecommendationId");

            migrationBuilder.AddColumn<string>(
                name: "ActionUrl",
                table: "ai_recommendations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDismissed",
                table: "ai_recommendations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "ai_recommendations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RecType",
                table: "ai_recommendations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_StudentId",
                table: "ai_recommendations",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_recommendations_students_StudentId",
                table: "ai_recommendations",
                column: "StudentId",
                principalTable: "students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
