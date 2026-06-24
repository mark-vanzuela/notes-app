using NotesApp.Application.Users;

namespace NotesApp.Application.Auth;

/// <summary>
/// What the API returns after a successful Google sign-in: the application's JWT
/// (which the frontend stores and sends on subsequent requests), when it expires,
/// and the signed-in user's profile.
/// </summary>
public record AuthResultDto(
    string Token,
    DateTime ExpiresAtUtc,
    UserDto User);
