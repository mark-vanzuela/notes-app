namespace NotesApp.Domain.Users;

/// <summary>
/// An application user. Users are NOT created by a registration form — they are
/// provisioned the first time someone signs in with Google (see the GoogleLogin
/// feature). The <see cref="GoogleSubjectId"/> is Google's stable, unique account
/// identifier (the "sub" claim) and is how we recognise a returning user.
///
/// Like every Domain entity, this lives in the innermost layer with zero
/// dependencies on EF Core, ASP.NET, or any other project. Private setters + a
/// static factory keep the entity in control of its own state.
/// </summary>
public class User
{
    public Guid Id { get; private set; }

    /// <summary>Google's "sub" claim — stable unique id for the Google account.</summary>
    public string GoogleSubjectId { get; private set; }

    public string Email { get; private set; }
    public string Name { get; private set; }

    /// <summary>Optional avatar URL from the Google profile.</summary>
    public string? PictureUrl { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Private parameterless constructor: EF Core uses it to materialise rows, and
    // it stops callers from creating an "empty" user. Use Create() instead.
    private User()
    {
        GoogleSubjectId = string.Empty;
        Email = string.Empty;
        Name = string.Empty;
    }

    /// <summary>Provisions a brand-new user from a verified Google profile.</summary>
    public static User Create(string googleSubjectId, string email, string name, string? pictureUrl)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = googleSubjectId,
            Email = email,
            Name = name,
            PictureUrl = pictureUrl,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    /// <summary>
    /// Refreshes the cached profile fields on a returning sign-in (the user may have
    /// changed their Google display name or picture since we last saw them).
    /// </summary>
    public void UpdateProfile(string email, string name, string? pictureUrl)
    {
        Email = email;
        Name = name;
        PictureUrl = pictureUrl;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
