using MediatR;
using Microsoft.Extensions.Logging;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Notes;
using NotesApp.Domain.Notes;

namespace NotesApp.Application.Features.Notes.CreateNote;

/// <summary>
/// Contains the business logic for creating one note. Dependencies (repository,
/// logger) arrive through the constructor via Dependency Injection.
/// </summary>
public class CreateNoteHandler : IRequestHandler<CreateNoteCommand, NoteDto>
{
    private readonly INoteRepository _repository;
    private readonly ILogger<CreateNoteHandler> _logger;

    public CreateNoteHandler(INoteRepository repository, ILogger<CreateNoteHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<NoteDto> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        var note = Note.Create(request.UserId, request.Title, request.Content);

        await _repository.AddAsync(note, cancellationToken);

        _logger.LogInformation("Created note {NoteId} for user {UserId}", note.Id, request.UserId);

        return note.ToDto();
    }
}
