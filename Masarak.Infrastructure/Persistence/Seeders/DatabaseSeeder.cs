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
        
        public static async Task SeedTestTeachersAsync(Context db, IPasswordService pwd)
        {
            if (await db.Teachers.AnyAsync()) return;

            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Teacher);
            if (role == null) return;

            var user1 = new User { RoleId = role.RoleId, FullName = "Ahmed Teacher", Email = "ahmed.teacher@masarak.com", PasswordHash = pwd.HashPassword("Teacher@123!"), Country = "EG", CreatedAt = DateTime.UtcNow, IsActive = true, EmailConfirmed = true, FailedLoginCount = 0 };
            var user2 = new User { RoleId = role.RoleId, FullName = "Mona Teacher", Email = "mona.teacher@masarak.com", PasswordHash = pwd.HashPassword("Teacher@123!"), Country = "EG", CreatedAt = DateTime.UtcNow, IsActive = true, EmailConfirmed = true, FailedLoginCount = 0 };
            
            db.Users.AddRange(user1, user2);
            await db.SaveChangesAsync();

            db.Teachers.Add(new Teacher { UserId = user1.UserId, Specialization = "Mathematics", Bio = "Math teacher", HiringDate = DateTime.UtcNow });
            db.Teachers.Add(new Teacher { UserId = user2.UserId, Specialization = "Science", Bio = "Science teacher", HiringDate = DateTime.UtcNow });
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] 2 test teachers seeded.");
        }

        public static async Task SeedTestStudentsAsync(Context db, IPasswordService pwd)
        {
            if (await db.Students.AnyAsync()) return;

            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Student);
            var grade1 = await db.Grades.FirstOrDefaultAsync(g => g.Order == 1);
            if (role == null || grade1 == null) return;

            var user1 = new User { RoleId = role.RoleId, FullName = "Youssef Student", Email = "youssef@student.com", PasswordHash = pwd.HashPassword("Student@123!"), Country = "EG", CreatedAt = DateTime.UtcNow, IsActive = true, EmailConfirmed = true, FailedLoginCount = 0 };
            var user2 = new User { RoleId = role.RoleId, FullName = "Salma Student", Email = "salma@student.com", PasswordHash = pwd.HashPassword("Student@123!"), Country = "EG", CreatedAt = DateTime.UtcNow, IsActive = true, EmailConfirmed = true, FailedLoginCount = 0 };
            
            db.Users.AddRange(user1, user2);
            await db.SaveChangesAsync();

            db.Students.Add(new Student { UserId = user1.UserId, GradeId = grade1.GradeId, EnrollmentDate = DateTime.UtcNow, AcademicStatus = "Active" });
            db.Students.Add(new Student { UserId = user2.UserId, GradeId = grade1.GradeId, EnrollmentDate = DateTime.UtcNow, AcademicStatus = "Active" });
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] 2 test students seeded.");
        }

        public static async Task SeedSubjectsAsync(Context db)
        {
            if (await db.Subjects.AnyAsync()) return;
            var grade1 = await db.Grades.FirstOrDefaultAsync(g => g.Order == 1);
            if (grade1 == null) return;

            db.Subjects.Add(Subject.Create(grade1.GradeId, "Mathematics 1", "رياضيات", "MATH-1"));
            db.Subjects.Add(Subject.Create(grade1.GradeId, "Science 1", "علوم", "SCI-1"));
            db.Subjects.Add(Subject.Create(grade1.GradeId, "Arabic 1", "عربي", "ARB-1"));
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] 3 test subjects seeded.");
        }

        public static async Task SeedClassesAsync(Context db)
        {
            if (await db.Classes.AnyAsync()) return;
            var grade1 = await db.Grades.FirstOrDefaultAsync(g => g.Order == 1);
            if (grade1 == null) return;

            db.Classes.Add(Class.Create(grade1.GradeId, "Class 1A", 30, 2024));
            db.Classes.Add(Class.Create(grade1.GradeId, "Class 1B", 30, 2024));
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] 2 test classes seeded.");
        }

        public static async Task SeedTeachingAssignmentsAsync(Context db)
        {
            if (await db.TeachingAssignments.AnyAsync()) return;
            var teacher1 = await db.Teachers.FirstOrDefaultAsync();
            var class1 = await db.Classes.FirstOrDefaultAsync();
            var mathSubj = await db.Subjects.FirstOrDefaultAsync(s => s.Name == "Mathematics 1");
            if (teacher1 == null || class1 == null || mathSubj == null) return;

            db.TeachingAssignments.Add(TeachingAssignment.Create(teacher1.TeacherId, class1.ClassId, mathSubj.SubjectId, 2024));
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] 1 teaching assignment seeded.");
        }

        public static async Task SeedStudentEnrollmentsAsync(Context db)
        {
            if (await db.StudentClasses.AnyAsync()) return;
            var student1 = await db.Students.FirstOrDefaultAsync();
            var class1 = await db.Classes.FirstOrDefaultAsync();
            if (student1 == null || class1 == null) return;

            db.StudentClasses.Add(StudentClass.Enroll(student1.StudentId, class1.ClassId, 2024));
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] 1 student enrollment seeded.");
        }
    }
}
