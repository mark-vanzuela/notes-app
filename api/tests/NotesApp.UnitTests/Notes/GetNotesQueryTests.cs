using FluentAssertions;
using Moq;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Features.Notes.GetNotes;
using NotesApp.Domain.Notes;

namespace NotesApp.UnitTests.Notes;

public class GetNotesQueryTests
{
    [Fact]
    public async Task Handle_ReturnsMappedDtos_ForTheUser()
    {
        var userId = Guid.NewGuid();
        IReadOnlyList<Note> notes = new[]
        {
            Note.Create(userId, "First", "A"),
            Note.Create(userId, "Second", "B")
        };

        var repository = new Mock<INoteRepository>();
        repository
            .Setup(r => r.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        var handler = new GetNotesHandler(repository.Object);
        var result = await handler.Handle(new GetNotesQuery(userId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(n => n.Title).Should().Contain(new[] { "First", "Second" });
    }
}
