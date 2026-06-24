using Microsoft.EntityFrameworkCore;
using NotesApp.Domain.Notes;
using NotesApp.Domain.Users;

namespace NotesApp.Infrastructure.Persistence;

/// <summary>
/// The EF Core DbContext — the bridge between our domain entities and Postgres.
/// It exposes one <see cref="DbSet{T}"/> per aggregate and configures how each
/// entity maps to a table in <see cref="OnModelCreating"/>.
/// </summary>
public class NotesDbContext : DbContext
{
    public NotesDbContext(DbContextOptions<NotesDbContext> options) : base(options)
    {
    }

    public DbSet<Note> Notes => Set<Note>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            // Google's "sub" is how we recognise a returning user, so it must be
            // unique and indexed.
            entity.Property(u => u.GoogleSubjectId).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.GoogleSubjectId).IsUnique();

            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.PictureUrl).HasMaxLength(2048);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Content).IsRequired();

            // A note belongs to exactly one user; deleting a user cascades to their
            // notes. UserId is indexed because we always query notes by owner.
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(n => n.UserId);

            // GLOBAL QUERY FILTER: every query against Notes automatically appends
            // "WHERE IsDeleted = false", so soft-deleted rows are invisible to the
            // rest of the app without each query remembering to filter.
            entity.HasQueryFilter(n => !n.IsDeleted);
        });
    }
}
