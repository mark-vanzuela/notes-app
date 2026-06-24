using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotesApp.Application.Abstractions;
using NotesApp.Infrastructure.Authentication;
using NotesApp.Infrastructure.Persistence;

namespace NotesApp.Infrastructure;

/// <summary>
/// Registers everything the Infrastructure layer provides: the Postgres-backed
/// DbContext, the EF Core repositories, and the auth helpers (Google token
/// verification + JWT issuance). Reads connection string + options from config.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Database ---------------------------------------------------------
        var connectionString = configuration.GetConnectionString("Default");
        services.AddDbContext<NotesDbContext>(options => options.UseNpgsql(connectionString));

        // Repositories are Scoped: one per HTTP request, matching the DbContext.
        services.AddScoped<INoteRepository, EfNoteRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();

        // --- Auth options + helpers ------------------------------------------
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
