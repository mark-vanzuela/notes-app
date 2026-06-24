using FluentValidation;

namespace NotesApp.Application.Features.Notes.CreateNote;

/// <summary>
/// FluentValidation rules for creating a note. The ValidationBehavior in the
/// MediatR pipeline runs this automatically before the handler. Keeping the rules
/// in the feature folder is the "vertical slice" idea: everything one feature needs
/// sits together.
/// </summary>
public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.");
    }
}
