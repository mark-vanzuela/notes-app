using NotesApp.Domain.Notes;

namespace NotesApp.Application.Notes;

/// <summary>
/// The SHAPE we send back over the API for a note. Kept separate from the domain
/// <see cref="Note"/> entity so the entity can protect its internals and the public
/// contract can stay stable. Note we deliberately do NOT expose UserId or IsDeleted
/// — those are internal concerns.
/// </summary>
public record NoteDto(
    Guid Id,
    string Title,
    string Content,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>Manual mapping (entity -> DTO), by hand so the data flow stays obvious.</summary>
public static class NoteMapping
{
    public static NoteDto ToDto(this Note note) => new(
        note.Id,
        note.Title,
        note.Content,
        note.CreatedAtUtc,
        note.UpdatedAtUtc);
}
