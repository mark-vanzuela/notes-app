using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using NotesApp.Application.Abstractions;

namespace NotesApp.Infrastructure.Authentication;

/// <summary>
/// Verifies a Google ID token using Google's official library. The validation
/// checks the token's signature against Google's public keys and confirms the
/// issuer, expiry, and — crucially — that the AUDIENCE matches OUR client id (so a
/// token minted for some other app cannot be replayed against us).
///
/// On any problem the underlying library throws <see cref="InvalidJwtException"/>,
/// which we let propagate; the API maps it to 401 Unauthorized.
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthOptions _options;

    public GoogleTokenValidator(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GoogleUserPayload> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            // The token's "aud" must equal our Google client id.
            Audience = new[] { _options.ClientId }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

        return new GoogleUserPayload(
            Subject: payload.Subject,
            Email: payload.Email ?? string.Empty,
            Name: payload.Name ?? payload.Email ?? "Unknown",
            PictureUrl: payload.Picture);
    }
}
