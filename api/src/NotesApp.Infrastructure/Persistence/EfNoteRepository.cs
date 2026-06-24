using Microsoft.EntityFrameworkCore;
using NotesApp.Application.Abstractions;
using NotesApp.Domain.Notes;

namespace NotesApp.Infrastructure.Persistence;

/// <summary>
/// The EF Core implementation of <see cref="INoteRepository"/>. This is the
/// "detail" that plugs into the abstraction the Application layer depends on —
/// swap it for another store and nothing else changes.
///
/// All reads are scoped by <c>userId</c> so a user only ever touches their own
/// notes. Soft-deleted rows are excluded automatically by the query filter on the
/// DbContext. Each write saves immediately (mirrors the reference repo's semantics).
/// </summary>
public class EfNoteRepository : INoteRepository
{
    private readonly NotesDbContext _db;

    public EfNoteRepository(NotesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Note>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Notes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Note?> GetByIdAsync(Guid userId, Guid noteId, CancellationToken cancellationToken = default)
    {
        return await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        await _db.Notes.AddAsync(note, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        // The note is already tracked (it was loaded via GetByIdAsync), so EF
        // detects the changes; we just persist them.
        _db.Notes.Update(note);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
