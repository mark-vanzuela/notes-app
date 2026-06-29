using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NotesApp.Api.Authentication;
using NotesApp.Api.Middleware;
using NotesApp.Application;
using NotesApp.Application.Abstractions;
using NotesApp.Infrastructure;
using NotesApp.Infrastructure.Authentication;
using NotesApp.Infrastructure.Persistence;

// Program.cs is the application's COMPOSITION ROOT: the single place where every
// layer is wired together and the HTTP pipeline is configured.

var builder = WebApplication.CreateBuilder(args);

// --- Console logging -------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- Register services (Dependency Injection) ------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger, configured so you can paste a Bearer token via the "Authorize" button.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Notes API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned by POST /api/auth/google.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

// Each layer contributes its own services through one extension method.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// The current user is resolved per-request from the JWT claims on HttpContext.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// --- Health checks ---------------------------------------------------------
// Liveness ("is the process up?") vs readiness ("can it actually serve traffic?",
// i.e. the database is reachable). Azure Container Apps points its liveness probe
// at /health/live and its readiness/startup probe at /health/ready. Keeping the DB
// check OUT of liveness avoids restart loops if the DB is briefly unavailable.
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<NotesDbContext>("database", tags: new[] { "ready" });

// --- Authentication / Authorization ----------------------------------------
// Validate the application's OWN JWT (issued by JwtTokenGenerator). The signing
// key + issuer/audience come from configuration ("Jwt" section).
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// --- CORS ------------------------------------------------------------------
// Allow any http://localhost:<port> origin for LOCAL DEV (Vite ports drift), plus
// any explicit production origins from config. In prod, Cors__AllowedOrigins is set
// to the deployed web app's URL (comma-separated for multiple).
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

const string FrontendCorsPolicy = "AllowLocalFrontends";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
                if (uri.Host == "localhost" || uri.Host == "127.0.0.1") return true;
                return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// --- Dev-only: apply EF Core migrations on startup -------------------------
// In Development we auto-migrate so `docker compose up` gives a ready database.
// Production applies migrations through the deploy pipeline (see CLAUDE.md).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
    db.Database.Migrate();
}

// --- HTTP request pipeline -------------------------------------------------
// Our custom error handler goes first so it can catch exceptions from everything
// after it.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Health endpoints — anonymous (no [Authorize]) so probes/monitors can reach them.
//   /health/live  -> liveness: 200 if the app is running (no dependency checks).
//   /health/ready -> readiness: 200 only if the "ready"-tagged checks (DB) pass.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapControllers();

app.Run();

// Exposed so integration tests (a future round) can reference the entry point.
public partial class Program { }
