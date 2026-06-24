using NotesApp.Domain.Notes;

namespace NotesApp.Application.Abstractions;

/// <summary>
/// The Repository Pattern: describes WHAT we can do with stored notes, not HOW or
/// WHERE they live. The Application layer depends only on this contract; the EF
/// Core implementation plugs in behind it from the Infrastructure layer.
///
/// Every method is scoped by <c>userId</c>: a user can only ever see and act on
/// their OWN notes. Soft-deleted notes are excluded automatically by a DbContext
/// query filter, so callers never see them.
/// </summary>
public interface INoteRepository
{
    /// <summary>All of this user's non-deleted notes, newest first.</summary>
    Task<IReadOnlyList<Note>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>One of this user's notes by id, or null if missing/not theirs/deleted.</summary>
    Task<Note?> GetByIdAsync(Guid userId, Guid noteId, CancellationToken cancellationToken = default);

    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to an existing tracked note.</summary>
    Task UpdateAsync(Note note, CancellationToken cancellationToken = default);
}
