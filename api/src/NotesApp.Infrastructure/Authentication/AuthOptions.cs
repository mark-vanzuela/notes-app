namespace NotesApp.Infrastructure.Authentication;

/// <summary>
/// Bound from configuration section "Authentication:Google". The ClientId is the
/// OAuth Web client id from Google Cloud Console; it is the expected AUDIENCE when
/// we verify an incoming Google ID token.
/// </summary>
public class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; set; } = string.Empty;
}

/// <summary>
/// Bound from configuration section "Jwt". Describes how we sign and validate the
/// application's OWN access tokens. <see cref="Key"/> is a secret (dev placeholder
/// in appsettings.Development.json; injected via env/Key Vault in production).
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
