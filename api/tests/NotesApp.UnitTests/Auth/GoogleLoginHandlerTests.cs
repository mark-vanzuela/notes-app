using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Features.Auth.GoogleLogin;
using NotesApp.Domain.Users;

namespace NotesApp.UnitTests.Auth;

/// <summary>
/// Tests for the Google sign-in handler. The Google SDK and JWT signing are behind
/// abstractions, so we mock them and verify the upsert + token-issuance logic.
/// </summary>
public class GoogleLoginHandlerTests
{
    private const string IdToken = "fake-google-id-token";
    private static readonly GoogleUserPayload Payload =
        new("google-sub-123", "ada@example.com", "Ada Lovelace", "https://pic/ada.png");

    private static GoogleLoginHandler BuildHandler(
        Mock<IUserRepository> users,
        Mock<IGoogleTokenValidator>? validator = null,
        Mock<IJwtTokenGenerator>? jwt = null)
    {
        validator ??= new Mock<IGoogleTokenValidator>();
        validator
            .Setup(v => v.ValidateAsync(IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Payload);

        jwt ??= new Mock<IJwtTokenGenerator>();
        jwt
            .Setup(j => j.Generate(It.IsAny<User>()))
            .Returns(new AccessToken("signed-jwt", DateTime.UtcNow.AddHours(1)));

        return new GoogleLoginHandler(validator.Object, users.Object, jwt.Object, NullLogger<GoogleLoginHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CreatesUser_OnFirstSignIn()
    {
        var users = new Mock<IUserRepository>();
        users
            .Setup(r => r.GetByGoogleSubjectIdAsync(Payload.Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = BuildHandler(users);
        var result = await handler.Handle(new GoogleLoginCommand(IdToken), CancellationToken.None);

        users.Verify(r => r.AddAsync(
            It.Is<User>(u => u.GoogleSubjectId == Payload.Subject && u.Email == Payload.Email),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Token.Should().Be("signed-jwt");
        result.User.Email.Should().Be("ada@example.com");
    }

    [Fact]
    public async Task Handle_ReusesExistingUser_OnReturningSignIn()
    {
        var existing = User.Create(Payload.Subject, "old@example.com", "Old Name", null);
        var users = new Mock<IUserRepository>();
        users
            .Setup(r => r.GetByGoogleSubjectIdAsync(Payload.Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = BuildHandler(users);
        var result = await handler.Handle(new GoogleLoginCommand(IdToken), CancellationToken.None);

        users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        users.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        // Profile is refreshed from the latest Google data.
        result.User.Email.Should().Be("ada@example.com");
        result.User.Id.Should().Be(existing.Id);
    }
}
