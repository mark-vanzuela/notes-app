namespace NotesApp.Application.Abstractions;

/// <summary>
/// Exposes the id of the user making the current request. The implementation
/// (in the API layer) reads it from the validated JWT claims on the HTTP context.
/// Handlers depend on this abstraction so business logic never touches HttpContext.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's id. Throws if there is no authenticated user.</summary>
    Guid Id { get; }
}
