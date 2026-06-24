using Microsoft.EntityFrameworkCore;
using NotesApp.Application.Abstractions;
using NotesApp.Domain.Users;

namespace NotesApp.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public class EfUserRepository : IUserRepository
{
    private readonly NotesDbContext _db;

    public EfUserRepository(NotesDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.GoogleSubjectId == googleSubjectId, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _db.Users.AddAsync(user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
