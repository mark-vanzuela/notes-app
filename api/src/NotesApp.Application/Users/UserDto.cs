using NotesApp.Domain.Users;

namespace NotesApp.Application.Users;

/// <summary>Public shape of a user — what the frontend needs to render "who am I".</summary>
public record UserDto(
    Guid Id,
    string Email,
    string Name,
    string? PictureUrl);

public static class UserMapping
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.Email,
        user.Name,
        user.PictureUrl);
}
