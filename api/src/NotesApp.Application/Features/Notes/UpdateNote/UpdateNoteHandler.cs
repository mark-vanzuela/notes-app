using MediatR;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Common.Exceptions;
using NotesApp.Application.Notes;

namespace NotesApp.Application.Features.Notes.UpdateNote;

public class UpdateNoteHandler : IRequestHandler<UpdateNoteCommand, NoteDto>
{
    private readonly INoteRepository _repository;

    public UpdateNoteHandler(INoteRepository repository)
    {
        _repository = repository;
    }

    public async Task<NoteDto> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
        // Scoped by userId: if the note is missing, deleted, or owned by someone
        // else, the repository returns null and we surface a 404.
        var note = await _repository.GetByIdAsync(request.UserId, request.NoteId, cancellationToken)
            ?? throw NotFoundException.ForNote(request.NoteId);

        note.Update(request.Title, request.Content);
        await _repository.UpdateAsync(note, cancellationToken);

        return note.ToDto();
    }
}
