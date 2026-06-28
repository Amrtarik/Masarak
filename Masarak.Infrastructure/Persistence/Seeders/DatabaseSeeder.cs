using Masarak.Domain.Constants;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Masarak.Application.Interfaces;
using Masarak.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Seeders
{
    public static class DatabaseSeeder
    {
        public static async Task SeedRolesAsync(Context db)
        {
            var roleNames = new[] { AppRoles.Admin, AppRoles.Teacher, AppRoles.Student, AppRoles.Parent };
            bool changed = false;
            foreach (var name in roleNames)
                if (!await db.Roles.AnyAsync(r => r.Name == name))
                { db.Roles.Add(new Role { Name = name }); changed = true; }
            if (changed) await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] Roles verified.");
        }

        public static async Task SeedAdminUserAsync(Context db, IPasswordService passwordService)
        {
            const string adminEmail = "admin@masarak.com";
            if (await db.Users.AnyAsync(u => u.Email == adminEmail))
            { Console.WriteLine("[Seeder] Admin exists. Skipped."); return; }

            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Admin)
                ?? throw new InvalidOperationException("[Seeder] Admin role missing.");

            db.Users.Add(new User
            {
                RoleId = adminRole.RoleId, FullName = "System Administrator",
                Email = adminEmail, PasswordHash = passwordService.HashPassword("Admin@12345!"),
                Country = "EG", CreatedAt = DateTime.UtcNow,
                IsActive = true, EmailConfirmed = true, FailedLoginCount = 0
            });
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] Admin created: admin@masarak.com / Admin@12345!  ← CHANGE THIS");
        }

        /// <summary>
        /// Seeds 12 Egyptian school grades with Arabic/English names and stage classification.
        /// Phase 2 Academic Core: replaces the simple Grade 1-12 seeder.
        /// </summary>
        public static async Task SeedGradesAsync(Context db)
        {
            if (await db.Grades.AnyAsync()) return;

            var grades = new List<Grade>
            {
                // Primary (ابتدائي) — Grades 1-6
                Grade.Create("Grade 1", "أولى ابتدائي",  GradeStage.Primary, 1),
                Grade.Create("Grade 2", "ثانية ابتدائي", GradeStage.Primary, 2),
                Grade.Create("Grade 3", "ثالثة ابتدائي", GradeStage.Primary, 3),
                Grade.Create("Grade 4", "رابعة ابتدائي", GradeStage.Primary, 4),
                Grade.Create("Grade 5", "خامسة ابتدائي", GradeStage.Primary, 5),
                Grade.Create("Grade 6", "سادسة ابتدائي", GradeStage.Primary, 6),

                // Preparatory (إعدادي) — Grades 7-9
                Grade.Create("Grade 7", "أولى إعدادي",  GradeStage.Preparatory, 7),
                Grade.Create("Grade 8", "ثانية إعدادي", GradeStage.Preparatory, 8),
                Grade.Create("Grade 9", "ثالثة إعدادي", GradeStage.Preparatory, 9),

                // Secondary (ثانوي) — Grades 10-12
                Grade.Create("Grade 10", "أولى ثانوي",  GradeStage.Secondary, 10),
                Grade.Create("Grade 11", "ثانية ثانوي", GradeStage.Secondary, 11),
                Grade.Create("Grade 12", "ثالثة ثانوي", GradeStage.Secondary, 12),
            };

            await db.Grades.AddRangeAsync(grades);
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] 12 Egyptian school grades seeded (Primary/Preparatory/Secondary).");
        }

        public static async Task SeedPlansAsync(Context db)
        {
            if (await db.Plans.AnyAsync()) return;

            var plans = new List<Plan>
            {
                new Plan
                {
                    Name = "Monthly",
                    Type = Masarak.Domain.Enums.PlanType.Monthly,
                    PriceMonthly = 9.99m,
                    Currency = "USD",
                    DurationDays = 30,
                    IsActive = true
                },
                new Plan
                {
                    Name = "Per-Subject",
                    Type = Masarak.Domain.Enums.PlanType.PerSubject,
                    PriceMonthly = 4.99m,
                    Currency = "USD",
                    DurationDays = 30,
                    IsActive = true
                },
                new Plan
                {
                    Name = "Full-Curriculum",
                    Type = Masarak.Domain.Enums.PlanType.FullCurriculum,
                    PriceMonthly = 24.99m,
                    Currency = "USD",
                    DurationDays = 365,
                    IsActive = true
                }
            };

            await db.Plans.AddRangeAsync(plans);
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] Default subscription plans seeded.");
        }

        /// <summary>
        /// Phase 4: Seeds one GradeCommunity chat room per grade and one TeachersCommunity room.
        /// Idempotent — only creates if not exists.
        /// </summary>
        public static async Task SeedChatRoomsAsync(Context db)
        {
            if (await db.ChatRooms.AnyAsync()) return;

            var grades = await db.Grades.OrderBy(g => g.Order).ToListAsync();
            var rooms = new List<Domain.Entities.ChatRoom>();

            foreach (var grade in grades)
            {
                rooms.Add(Domain.Entities.ChatRoom.CreateGradeCommunity(grade.GradeId, $"{grade.Name} Community"));
            }

            rooms.Add(Domain.Entities.ChatRoom.CreateTeachersCommunity());

            await db.ChatRooms.AddRangeAsync(rooms);
            await db.SaveChangesAsync();
            Console.WriteLine($"[Seeder] {rooms.Count} chat rooms seeded ({grades.Count} grade rooms + 1 teachers room).");
        }

        /// <summary>
        /// Phase 5: Seeds default AI prompt templates for weakness analysis, parent reports, and teaching suggestions.
        /// Idempotent — only creates if not exists.
        /// </summary>
        public static async Task SeedAiPromptTemplatesAsync(Context db)
        {
            if (await db.AiPromptTemplates.AnyAsync()) return;

            var templates = new List<AiPromptTemplate>
            {
                new AiPromptTemplate
                {
                    Key = "weakness_analysis",
                    SystemPrompt = "You are an educational analyst for Egyptian K-12 curriculum. Analyze student performance data and identify specific topic weaknesses. Respond only in JSON.",
                    UserPromptTemplate = "Student: {student_name}, Subject: {subject_name}, Grade: {grade_name}. Exam results: {exam_results_json}. Assignment results: {assignment_results_json}. Identify top 3 weak topics, provide error rate per topic (0-1), and 2 specific recommended actions per topic in {language}. Return JSON matching schema: {schema}.",
                    MaxTokens = 1000,
                    Temperature = 0.3m,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AiPromptTemplate
                {
                    Key = "parent_report",
                    SystemPrompt = "You are a student academic advisor writing a monthly report for an Egyptian parent. Be encouraging, clear, and specific. Write in {language}.",
                    UserPromptTemplate = "Student: {student_name}, Report month: {month}. Performance data: {performance_json}. Attendance: {attendance_json}. Write a 200-word narrative summary and 3 specific recommended actions for the parent.",
                    MaxTokens = 800,
                    Temperature = 0.7m,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AiPromptTemplate
                {
                    Key = "teaching_suggestion",
                    SystemPrompt = "You are a curriculum expert for Egyptian K-12 education. Provide concise, actionable teaching suggestions.",
                    UserPromptTemplate = "Teacher subject: {subject_name}, Grade: {grade_name}, Student weak topics: {weak_topics_json}. Provide one targeted teaching suggestion and 3 specific action items.",
                    MaxTokens = 500,
                    Temperature = 0.5m,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                }
            };

            await db.AiPromptTemplates.AddRangeAsync(templates);
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] 3 AI prompt templates seeded (weakness_analysis, parent_report, teaching_suggestion).");
        }
    }
}
