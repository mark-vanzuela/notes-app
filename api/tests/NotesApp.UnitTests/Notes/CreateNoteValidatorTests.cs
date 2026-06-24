using FluentAssertions;
using NotesApp.Application.Features.Notes.CreateNote;

namespace NotesApp.UnitTests.Notes;

public class CreateNoteValidatorTests
{
    private readonly CreateNoteValidator _validator = new();

    [Fact]
    public void Passes_ForValidCommand()
    {
        var result = _validator.Validate(new CreateNoteCommand(Guid.NewGuid(), "Title", "Body"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Body")]      // empty title
    [InlineData("Title", "")]     // empty content
    public void Fails_WhenRequiredFieldMissing(string title, string content)
    {
        var result = _validator.Validate(new CreateNoteCommand(Guid.NewGuid(), title, content));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fails_WhenTitleTooLong()
    {
        var result = _validator.Validate(new CreateNoteCommand(Guid.NewGuid(), new string('a', 201), "Body"));
        result.IsValid.Should().BeFalse();
    }
}
