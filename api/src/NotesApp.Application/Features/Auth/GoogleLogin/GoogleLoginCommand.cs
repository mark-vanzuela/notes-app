using MediatR;
using NotesApp.Application.Auth;

namespace NotesApp.Application.Features.Auth.GoogleLogin;

/// <summary>
/// Exchanges a Google ID token (obtained by the frontend via Google Identity
/// Services) for the application's own JWT. First time we see a Google account this
/// also provisions the user — sign-up and sign-in are the same action.
/// </summary>
public record GoogleLoginCommand(string IdToken) : IRequest<AuthResultDto>;
