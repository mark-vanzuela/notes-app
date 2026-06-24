using MediatR;
using Microsoft.Extensions.Logging;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Auth;
using NotesApp.Application.Users;
using NotesApp.Domain.Users;

namespace NotesApp.Application.Features.Auth.GoogleLogin;

/// <summary>
/// The heart of authentication:
///   1. Verify the Google ID token (signature/issuer/audience/expiry).
///   2. Look up the user by their Google subject id.
///   3. If new -> create them (sign-up); if returning -> refresh their profile.
///   4. Issue OUR application JWT and return it with the user's profile.
///
/// The handler depends only on abstractions (token validator, repository, JWT
/// generator) — the concrete Google/JWT details live in the Infrastructure layer.
/// </summary>
public class GoogleLoginHandler : IRequestHandler<GoogleLoginCommand, AuthResultDto>
{
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IUserRepository _users;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ILogger<GoogleLoginHandler> _logger;

    public GoogleLoginHandler(
        IGoogleTokenValidator googleTokenValidator,
        IUserRepository users,
        IJwtTokenGenerator jwt,
        ILogger<GoogleLoginHandler> logger)
    {
        _googleTokenValidator = googleTokenValidator;
        _users = users;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<AuthResultDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify with Google. Throws if the token is invalid -> mapped to 401.
        var payload = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);

        // 2 & 3. Upsert the user keyed by Google's stable subject id.
        var user = await _users.GetByGoogleSubjectIdAsync(payload.Subject, cancellationToken);
        if (user is null)
        {
            user = User.Create(payload.Subject, payload.Email, payload.Name, payload.PictureUrl);
            await _users.AddAsync(user, cancellationToken);
            _logger.LogInformation("Registered new user {UserId} ({Email}) via Google", user.Id, user.Email);
        }
        else
        {
            user.UpdateProfile(payload.Email, payload.Name, payload.PictureUrl);
            await _users.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("User {UserId} ({Email}) signed in via Google", user.Id, user.Email);
        }

        // 4. Mint our own JWT for subsequent API calls.
        var token = _jwt.Generate(user);

        return new AuthResultDto(token.Token, token.ExpiresAtUtc, user.ToDto());
    }
}
