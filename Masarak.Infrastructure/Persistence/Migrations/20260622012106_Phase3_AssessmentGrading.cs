using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masarak.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_AssessmentGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exams_AssignmentId",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "questions");

            migrationBuilder.RenameColumn(
                name: "QuestionType",
                table: "questions",
                newName: "Type");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "submissions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Submitted",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Submitted");

            migrationBuilder.AddColumn<string>(
                name: "FileBlobName",
                table: "submissions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "submissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "student_performance",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalAssignmentsPending",
                table: "student_performance",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalAssignmentsSubmitted",
                table: "student_performance",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalExamsTaken",
                table: "student_performance",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "student_exams",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "InProgress",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "student_exams",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "FinalScore",
                table: "student_exams",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasPendingManualGrading",
                table: "student_exams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAutoScore",
                table: "student_exams",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalManualScore",
                table: "student_exams",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileBlobName",
                table: "student_answers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "student_answers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradingStatus",
                table: "student_answers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PendingReview");

            migrationBuilder.AddColumn<string>(
                name: "SelectedOptionId",
                table: "student_answers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeacherFeedback",
                table: "student_answers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ExamId",
                table: "questions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "questions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "questions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionBankId",
                table: "questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "exams",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "exams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "exams",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalMarks",
                table: "exams",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "assignments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.CreateTable(
                name: "question_options",
                columns: table => new
                {
                    QuestionOptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_options", x => x.QuestionOptionId);
                    table.ForeignKey(
                        name: "FK_question_options_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_student_answers_GradingStatus",
                table: "student_answers",
                column: "GradingStatus",
                filter: "[GradingStatus] = 'PendingReview'");

            migrationBuilder.CreateIndex(
                name: "IX_exams_AssignmentId_StartTime",
                table: "exams",
                columns: new[] { "AssignmentId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_question_options_QuestionId",
                table: "question_options",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "question_options");

            migrationBuilder.DropIndex(
                name: "IX_student_answers_GradingStatus",
                table: "student_answers");

            migrationBuilder.DropIndex(
                name: "IX_exams_AssignmentId_StartTime",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "FileBlobName",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "student_performance");

            migrationBuilder.DropColumn(
                name: "TotalAssignmentsPending",
                table: "student_performance");

            migrationBuilder.DropColumn(
                name: "TotalAssignmentsSubmitted",
                table: "student_performance");

            migrationBuilder.DropColumn(
                name: "TotalExamsTaken",
                table: "student_performance");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "student_exams");

            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "student_exams");

            migrationBuilder.DropColumn(
                name: "HasPendingManualGrading",
                table: "student_exams");

            migrationBuilder.DropColumn(
                name: "TotalAutoScore",
                table: "student_exams");

            migrationBuilder.DropColumn(
                name: "TotalManualScore",
                table: "student_exams");

            migrationBuilder.DropColumn(
                name: "FileBlobName",
                table: "student_answers");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "student_answers");

            migrationBuilder.DropColumn(
                name: "GradingStatus",
                table: "student_answers");

            migrationBuilder.DropColumn(
                name: "SelectedOptionId",
                table: "student_answers");

            migrationBuilder.DropColumn(
                name: "TeacherFeedback",
                table: "student_answers");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "QuestionBankId",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "TotalMarks",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "assignments");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "questions",
                newName: "QuestionType");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "submissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Submitted",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Submitted");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "student_exams",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "InProgress");

            migrationBuilder.AlterColumn<int>(
                name: "ExamId",
                table: "questions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_exams_AssignmentId",
                table: "exams",
                column: "AssignmentId");
        }
    }
}
