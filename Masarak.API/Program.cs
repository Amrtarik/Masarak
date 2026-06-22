using Masarak.API;
using Masarak.API.Extensions;
using Masarak.Infrastructure.Persistence;
using Masarak.Infrastructure.Persistence.Seeders;
using Masarak.Application.Interfaces;
using Masarak.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// ── Database (uses Data/Context.cs with all 26 entities) ─────────────────────
builder.Services.AddDbContext<Context>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => {
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            sql.MigrationsAssembly("Masarak.Infrastructure");
        }));

// ── HTTP Context Accessor (needed by LocalFileStorageService) ────────────────
builder.Services.AddHttpContextAccessor();

// ── Application Services ──
builder.Services.AddApplicationServices(builder.Configuration);

// ── Background Jobs ──
builder.Services.AddHostedService<Masarak.Infrastructure.Services.ExamAutoExpireJob>();

// ── MassTransit ──
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<Masarak.Infrastructure.Messaging.ExamGradedConsumer>();
    x.AddConsumer<Masarak.Infrastructure.Messaging.AssignmentGradedConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services.AddJwtAuthentication(builder.Configuration);

// ── Authorization Policies ────────────────────────────────────────────────────
builder.Services.AddAuthorizationPolicies();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings ("Active", "Pending") not integers (0, 1)
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ── Swagger with JWT support ──────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins")
                       .Get<string[]>() ?? ["http://localhost:3000"])
              .AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Seed on startup ───────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db  = scope.ServiceProvider.GetRequiredService<Context>();
    var pwd = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    await db.Database.MigrateAsync();          // apply pending migrations
    await DatabaseSeeder.SeedRolesAsync(db);
    await DatabaseSeeder.SeedGradesAsync(db);  // needed for student self-registration
    await DatabaseSeeder.SeedAdminUserAsync(db, pwd);
    await DatabaseSeeder.SeedPlansAsync(db);   // Phase 1 plans
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Masarak API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();   // ← must come before UseAuthorization
app.UseAuthorization();
app.UseMiddleware<Masarak.API.Extensions.SubscriptionAccessMiddleware>();
app.MapControllers();
app.Run();
