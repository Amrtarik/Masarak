using Masarak.API;
using Masarak.API.Extensions;
using Masarak.Infrastructure.Persistence;
using Masarak.Infrastructure.Persistence.Seeders;
using Masarak.Application.Interfaces;
using Masarak.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Microsoft.AspNetCore.RateLimiting;

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
builder.Services.AddHostedService<Masarak.Infrastructure.Services.AbsentMarkingBackgroundJob>();

// ── SignalR (Phase 4: ChatHub) ────────────────────────────────────────────────
var redisConn = builder.Configuration.GetConnectionString("Redis");
var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(redisConn))
{
    signalRBuilder.AddStackExchangeRedis(redisConn, options =>
    {
        options.Configuration.ChannelPrefix = new StackExchange.Redis.RedisChannel("MasarakSignalR", StackExchange.Redis.RedisChannel.PatternMode.Literal);
    });
}

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

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("FileUploadPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5; // Max 5 uploads per minute
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

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
    await DatabaseSeeder.SeedChatRoomsAsync(db); // Phase 4 chat rooms
    if (app.Environment.IsDevelopment())
    {
        await DatabaseSeeder.SeedTestTeachersAsync(db, pwd);
        await DatabaseSeeder.SeedTestStudentsAsync(db, pwd);
        await DatabaseSeeder.SeedSubjectsAsync(db);
        await DatabaseSeeder.SeedClassesAsync(db);
        await DatabaseSeeder.SeedTeachingAssignmentsAsync(db);
        await DatabaseSeeder.SeedStudentEnrollmentsAsync(db);
    }
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
app.UseRateLimiter();
app.UseAuthentication();   // ← must come before UseAuthorization
app.UseAuthorization();
app.UseMiddleware<Masarak.API.Extensions.SubscriptionAccessMiddleware>();
app.MapControllers();
app.MapHub<Masarak.API.Hubs.ChatHub>("/hubs/chat"); // Phase 4: SignalR ChatHub
app.Run();

public partial class Program { }
