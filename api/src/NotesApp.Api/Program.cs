using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
// For LOCAL DEVELOPMENT allow any http://localhost:<port> origin so the React dev
// server (Vite, port may drift) can call the API. Lock this down in production.
const string FrontendCorsPolicy = "AllowLocalFrontends";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
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

app.MapControllers();

app.Run();

// Exposed so integration tests (a future round) can reference the entry point.
public partial class Program { }
