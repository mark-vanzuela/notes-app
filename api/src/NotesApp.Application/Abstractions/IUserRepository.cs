using NotesApp.Domain.Users;

namespace NotesApp.Application.Abstractions;

/// <summary>
/// Storage contract for users. Users are looked up by their Google subject id
/// during sign-in (to recognise a returning user) and created on first sight.
/// </summary>
public interface IUserRepository
{
    /// <summary>Finds a user by Google's stable "sub" id, or null if unknown.</summary>
    Task<User?> GetByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to an existing tracked user (profile refresh).</summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
