using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApp.Api.Contracts;
using NotesApp.Application.Auth;
using NotesApp.Application.Features.Auth.GoogleLogin;

namespace NotesApp.Api.Controllers;

/// <summary>
/// Authentication endpoints. This controller is [AllowAnonymous] because it is how
/// a caller GETS a token in the first place — everything else in the API requires
/// the JWT this returns.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// POST /api/auth/google — exchange a Google ID token for the app's JWT.
    /// Creates the user on first sign-in.
    /// </summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResultDto>> Google([FromBody] GoogleLoginRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new GoogleLoginCommand(request.IdToken), ct);
        return Ok(result);
    }
}
