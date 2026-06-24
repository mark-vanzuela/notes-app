using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Features.Notes.CreateNote;
using NotesApp.Domain.Notes;

namespace NotesApp.UnitTests.Notes;

/// <summary>
/// Unit tests for the create handler. We MOCK the repository with Moq so the test
/// is isolated from any real storage — we only verify the handler's own logic.
/// </summary>
public class CreateNoteHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDto_WithProvidedValues()
    {
        var repository = new Mock<INoteRepository>();
        var handler = new CreateNoteHandler(repository.Object, NullLogger<CreateNoteHandler>.Instance);
        var userId = Guid.NewGuid();
        var command = new CreateNoteCommand(userId, "Groceries", "Milk and eggs");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Groceries");
        result.Content.Should().Be("Milk and eggs");
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SavesNote_OwnedByTheRequestingUser()
    {
        var repository = new Mock<INoteRepository>();
        var handler = new CreateNoteHandler(repository.Object, NullLogger<CreateNoteHandler>.Instance);
        var userId = Guid.NewGuid();
        var command = new CreateNoteCommand(userId, "Groceries", "Milk and eggs");

        await handler.Handle(command, CancellationToken.None);

        // The handler must persist exactly one note, owned by the requesting user.
        repository.Verify(r => r.AddAsync(
            It.Is<Note>(n => n.UserId == userId && n.Title == "Groceries"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
