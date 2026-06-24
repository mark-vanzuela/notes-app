using FluentValidation;

namespace NotesApp.Application.Features.Auth.GoogleLogin;

public class GoogleLoginValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("A Google ID token is required.");
    }
}
