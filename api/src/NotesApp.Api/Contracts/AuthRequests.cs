namespace NotesApp.Api.Contracts;

/// <summary>
/// JSON body for POST /api/auth/google. The frontend obtains <paramref name="IdToken"/>
/// from Google Identity Services and posts it here to exchange for an app JWT.
/// </summary>
public record GoogleLoginRequest(string IdToken);
