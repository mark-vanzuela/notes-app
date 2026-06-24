using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NotesApp.Infrastructure.Persistence;

/// <summary>
/// Lets the EF Core command-line tools (`dotnet ef migrations add`,
/// `dotnet ef database update`) build a <see cref="NotesDbContext"/> at DESIGN time
/// without booting the whole API host. Generating a migration only needs the model
/// shape, so any well-formed Npgsql connection string works here — it does not have
/// to point at a running database. An env override is supported for applying
/// migrations against a real database from the CLI.
/// </summary>
public class NotesDbContextFactory : IDesignTimeDbContextFactory<NotesDbContext>
{
    public NotesDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=notesdb;Username=notes;Password=notes_dev_password";

        var options = new DbContextOptionsBuilder<NotesDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new NotesDbContext(options);
    }
}
