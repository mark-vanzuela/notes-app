using NotesApp.Domain.Users;

namespace NotesApp.Application.Abstractions;

/// <summary>A freshly minted application JWT plus when it expires.</summary>
public record AccessToken(string Token, DateTime ExpiresAtUtc);

/// <summary>
/// Issues the application's OWN JWT for an authenticated user. After we verify a
/// Google login we hand the caller one of these; every later request carries it as
/// a Bearer token. The implementation (Infrastructure) signs it with a configured
/// key — the Application layer stays free of token-format details.
/// </summary>
public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}
