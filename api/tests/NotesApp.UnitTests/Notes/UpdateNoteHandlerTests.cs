using FluentAssertions;
using Moq;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Common.Exceptions;
using NotesApp.Application.Features.Notes.UpdateNote;
using NotesApp.Domain.Notes;

namespace NotesApp.UnitTests.Notes;

public class UpdateNoteHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesAndPersists_WhenNoteExists()
    {
        var userId = Guid.NewGuid();
        var note = Note.Create(userId, "Old", "Old body");
        var repository = new Mock<INoteRepository>();
        repository
            .Setup(r => r.GetByIdAsync(userId, note.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        var handler = new UpdateNoteHandler(repository.Object);
        var result = await handler.Handle(new UpdateNoteCommand(userId, note.Id, "New", "New body"), CancellationToken.None);

        result.Title.Should().Be("New");
        result.Content.Should().Be("New body");
        repository.Verify(r => r.UpdateAsync(note, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws404_WhenNoteNotFoundForUser()
    {
        var userId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        var repository = new Mock<INoteRepository>();
        repository
            .Setup(r => r.GetByIdAsync(userId, missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Note?)null);

        var handler = new UpdateNoteHandler(repository.Object);
        var act = () => handler.Handle(new UpdateNoteCommand(userId, missingId, "New", "Body"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        repository.Verify(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
