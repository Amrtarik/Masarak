using Masarak.Domain.Constants;
using Masarak.Domain.Entities;
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

        public static async Task SeedGradesAsync(Context db)
        {
            if (await db.Grades.AnyAsync()) return;
            var grades = Enumerable.Range(1, 12)
                .Select(i => new Grade { Name = $"Grade {i}", Level = i }).ToList();
            await db.Grades.AddRangeAsync(grades);
            await db.SaveChangesAsync();
            Console.WriteLine("[Seeder] Grades 1-12 seeded.");
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
    }
}
