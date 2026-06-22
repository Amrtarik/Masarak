using Masarak.Domain.Constants;
using System.Text;
using Masarak.Infrastructure.Configurations;
using Masarak.API.Policies;
using Masarak.Application.Interfaces;
using Masarak.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Masarak.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSection);

            var jwtSettings = jwtSection.Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings not configured.");

            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = true; // Enforce HTTPS in production
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey  = true,
                        ValidIssuer              = jwtSettings.Issuer,
                        ValidAudience            = jwtSettings.Audience,
                        IssuerSigningKey         = new SymmetricSecurityKey(key),
                        ClockSkew                = TimeSpan.Zero  // No slack on expiry
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception is SecurityTokenExpiredException)
                                context.Response.Headers.Append("Token-Expired", "true");
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Single-role policies
                options.AddPolicy(AppPolicies.AdminOnly,
                    p => p.RequireRole(AppRoles.Admin));

                options.AddPolicy(AppPolicies.TeacherOnly,
                    p => p.RequireRole(AppRoles.Teacher));

                options.AddPolicy(AppPolicies.StudentOnly,
                    p => p.RequireRole(AppRoles.Student));

                options.AddPolicy(AppPolicies.ParentOnly,
                    p => p.RequireRole(AppRoles.Parent));

                // Combined policies
                options.AddPolicy(AppPolicies.AdminOrTeacher,
                    p => p.RequireRole(AppRoles.Admin, AppRoles.Teacher));

                options.AddPolicy(AppPolicies.StudentOrParent,
                    p => p.RequireRole(AppRoles.Student, AppRoles.Parent));

                options.AddPolicy(AppPolicies.AnyAuthenticated,
                    p => p.RequireAuthenticatedUser());
            });

            return services;
        }

        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title   = "Masarak Education API",
                    Version = "v1",
                    Description = "Educational Management System – Phase 2 (Auth & Authorization)"
                });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name         = "Authorization",
                    Type         = SecuritySchemeType.Http,
                    Scheme       = "bearer",
                    BearerFormat = "JWT",
                    In           = ParameterLocation.Header,
                    Description  = "Enter your JWT token. Example: eyJhbGci..."
                };

                options.AddSecurityDefinition("Bearer", securityScheme);

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Core
            services.AddMemoryCache(); // Keep this for other non-distributed cache usages if any
            
            // Distributed Cache (Redis)
            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = "Masarak_";
                });
            }
            else
            {
                // Fallback for development if Redis is not available
                services.AddDistributedMemoryCache();
            }
            
            // Auth Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IPasswordService, PasswordService>();

            // Email Service
            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
            services.AddScoped<IEmailService, EmailService>();

            // Subscription Services
            services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));
            services.AddScoped<ISubscriptionRepository, Masarak.Infrastructure.Persistence.Repositories.SubscriptionRepository>();
            services.AddScoped<IPlanRepository, Masarak.Infrastructure.Persistence.Repositories.PlanRepository>();
            services.AddScoped<IPaymentRepository, Masarak.Infrastructure.Persistence.Repositories.PaymentRepository>();
            services.AddScoped<IParentStudentLinkRepository, Masarak.Infrastructure.Persistence.Repositories.ParentStudentLinkRepository>();
            services.AddScoped<IUserRepository, Masarak.Infrastructure.Persistence.Repositories.UserRepository>();

            services.AddScoped<IStripeService, StripeService>();
            services.AddScoped<ISubscriptionAccessService, SubscriptionAccessService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();

            // ── Phase 2 Academic Core ─────────────────────────────────────────
            services.AddScoped<IGradeRepository, Masarak.Infrastructure.Persistence.Repositories.GradeRepository>();
            services.AddScoped<IClassRepository, Masarak.Infrastructure.Persistence.Repositories.ClassRepository>();
            services.AddScoped<ISubjectRepository, Masarak.Infrastructure.Persistence.Repositories.SubjectRepository>();
            services.AddScoped<ITeachingAssignmentRepository, Masarak.Infrastructure.Persistence.Repositories.TeachingAssignmentRepository>();
            services.AddScoped<IStudentClassRepository, Masarak.Infrastructure.Persistence.Repositories.StudentClassRepository>();
            services.AddScoped<ISessionRepository, Masarak.Infrastructure.Persistence.Repositories.SessionRepository>();

            services.AddSingleton<Masarak.Domain.Services.ScheduleConflictChecker>();
            services.AddScoped<IAcademicService, AcademicService>();
            services.AddScoped<ISessionService, SessionService>();

            // ── Phase 3 Assessment Core ───────────────────────────────────────
            services.AddScoped<IAssignmentRepository, Masarak.Infrastructure.Persistence.Repositories.AssignmentRepository>();
            services.AddScoped<IExamRepository, Masarak.Infrastructure.Persistence.Repositories.ExamRepository>();
            services.AddScoped<IStudentExamRepository, Masarak.Infrastructure.Persistence.Repositories.StudentExamRepository>();
            services.AddScoped<ISubmissionRepository, Masarak.Infrastructure.Persistence.Repositories.SubmissionRepository>();
            services.AddScoped<IStudentPerformanceRepository, Masarak.Infrastructure.Persistence.Repositories.StudentPerformanceRepository>();
            
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IAssessmentService, AssessmentService>();
            
            return services;
        }
    }
}
