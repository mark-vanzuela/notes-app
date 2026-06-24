namespace NotesApp.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist (or is not visible to the current
/// user). The API layer translates this into an HTTP 404 response. Using a
/// dedicated exception type keeps the handlers clean — they just throw, and the
/// edge maps it.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException ForNote(Guid id) =>
        new($"Note with id '{id}' was not found.");
}
