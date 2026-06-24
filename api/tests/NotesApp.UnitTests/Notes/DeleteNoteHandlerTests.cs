using FluentAssertions;
using Moq;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Common.Exceptions;
using NotesApp.Application.Features.Notes.DeleteNote;
using NotesApp.Domain.Notes;

namespace NotesApp.UnitTests.Notes;

public class DeleteNoteHandlerTests
{
    [Fact]
    public async Task Handle_SoftDeletesAndPersists_WhenNoteExists()
    {
        var userId = Guid.NewGuid();
        var note = Note.Create(userId, "Title", "Body");
        var repository = new Mock<INoteRepository>();
        repository
            .Setup(r => r.GetByIdAsync(userId, note.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        var handler = new DeleteNoteHandler(repository.Object);
        await handler.Handle(new DeleteNoteCommand(userId, note.Id), CancellationToken.None);

        note.IsDeleted.Should().BeTrue();
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

        var handler = new DeleteNoteHandler(repository.Object);
        var act = () => handler.Handle(new DeleteNoteCommand(userId, missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
