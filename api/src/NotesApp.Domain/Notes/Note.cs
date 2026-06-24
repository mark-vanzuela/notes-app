namespace NotesApp.Domain.Notes;

/// <summary>
/// The core business entity — a single note. Each note is OWNED by a user
/// (<see cref="UserId"/>); the application only ever shows a user their own notes.
///
/// Soft-delete approach (same as the reference CustomerApi): we never physically
/// remove a note. Instead we flip <see cref="IsDeleted"/>, so the row survives for
/// history but is hidden from normal queries (enforced by a global query filter in
/// the DbContext).
/// </summary>
public class Note
{
    public Guid Id { get; private set; }

    /// <summary>The owning user's id. Set once at creation; never reassigned.</summary>
    public Guid UserId { get; private set; }

    public string Title { get; private set; }
    public string Content { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Private parameterless constructor for EF Core materialisation + to force
    // callers through the factory method.
    private Note()
    {
        Title = string.Empty;
        Content = string.Empty;
    }

    /// <summary>Creates a new note owned by <paramref name="userId"/>.</summary>
    public static Note Create(Guid userId, string title, string content)
    {
        var now = DateTime.UtcNow;
        return new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Content = content,
            IsDeleted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    /// <summary>Applies an edit and bumps the "updated" timestamp.</summary>
    public void Update(string title, string content)
    {
        Title = title;
        Content = content;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Soft delete: hide the note without destroying it.</summary>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
