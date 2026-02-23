using InternalDashboard.API.Middleware;
using InternalDashboard.Core.Services;
using InternalDashboard.Infrastructure.Data;
using InternalDashboard.Infrastructure.Data.Seed;
using InternalDashboard.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Suppress EF SQL query logs in Production (appsettings.json sets Warning,
//    but appsettings.Development.json overrides to Information when merged).
//    Calling ConfigureLogging here ensures it wins regardless of environment. ──
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
}

// ── Validate required config early — fail fast before any service is built ────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not set. " +
        "In production set the environment variable: ConnectionStrings__DefaultConnection");

// ── Normalise the connection string ──────────────────────────────────────────
// Trim invisible chars (BOM, newlines, zero-width spaces) from Railway paste.
connectionString = connectionString.Trim();

// Convert postgres:// / postgresql:// URI → Npgsql key-value format.
// Strip ALL query params (e.g. ?sslmode=require) before parsing — System.Uri
// chokes on malformed query params like bare "?sslmode" with no value.
// SSL is hardcoded below; it is always required for Neon.
if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
    connectionString.StartsWith("postgres://",   StringComparison.OrdinalIgnoreCase))
{
    // Remove query string before handing to Uri parser
    var queryIndex = connectionString.IndexOf('?');
    if (queryIndex >= 0)
        connectionString = connectionString[..queryIndex];

    var uri      = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);          // limit 2 — password may contain ':'
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var database = uri.AbsolutePath.TrimStart('/');
    var port     = uri.Port > 0 ? uri.Port : 5432;

    connectionString =
        $"Host={uri.Host};Port={port};Database={database};" +
        $"Username={username};Password={password};" +
        $"SSL Mode=Require;Trust Server Certificate=true";
}

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("AllowedCorsOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddSingleton<IFailureSimulationService, FailureSimulationService>();

// ── Controllers & OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<GlobalExceptionMiddleware>(); // must be first — wraps everything below
app.UseCors("FrontendPolicy");

// HTTPS redirection is intentionally disabled.
// Railway (and most PaaS hosts) terminate TLS at the load balancer;
// the app only ever receives plain HTTP internally, so redirecting
// would cause redirect loops or broken requests.
app.UseAuthorization();
app.MapControllers();

// ── Seed the database on startup (skips if data already exists) ───────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseSeeder.SeedAsync(db, logger);
}

app.Run();

// Needed for integration test project to reference Program
public partial class Program { }
