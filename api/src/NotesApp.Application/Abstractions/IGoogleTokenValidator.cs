namespace NotesApp.Application.Abstractions;

/// <summary>
/// The verified payload of a Google ID token — only the fields we care about.
/// </summary>
public record GoogleUserPayload(string Subject, string Email, string Name, string? PictureUrl);

/// <summary>
/// Verifies a Google ID token (signature, issuer, audience, expiry) and returns
/// the trusted profile. The Application layer depends on this abstraction so it
/// never references Google's SDK directly — the implementation lives in
/// Infrastructure. Implementations should throw if the token is invalid.
/// </summary>
public interface IGoogleTokenValidator
{
    Task<GoogleUserPayload> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
