using System.Security.Claims;
using NotesApp.Application.Abstractions;

namespace NotesApp.Api.Authentication;

/// <summary>
/// Reads the authenticated user's id out of the JWT claims on the current HTTP
/// request. This is the API-layer implementation of <see cref="ICurrentUser"/>, so
/// handlers can ask "who is calling?" without ever touching HttpContext.
///
/// The id lives in the "sub" claim, which the JWT bearer handler surfaces as
/// <see cref="ClaimTypes.NameIdentifier"/>.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid Id
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(value, out var id))
            {
                return id;
            }

            // Reaching here means the endpoint ran without a valid authenticated
            // user — a wiring bug, since note endpoints are [Authorize]d.
            throw new InvalidOperationException("No authenticated user id is available on the request.");
        }
    }
}
