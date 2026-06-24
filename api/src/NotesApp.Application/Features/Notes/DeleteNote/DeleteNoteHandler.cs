using MediatR;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Common.Exceptions;

namespace NotesApp.Application.Features.Notes.DeleteNote;

public class DeleteNoteHandler : IRequestHandler<DeleteNoteCommand, Unit>
{
    private readonly INoteRepository _repository;

    public DeleteNoteHandler(INoteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByIdAsync(request.UserId, request.NoteId, cancellationToken)
            ?? throw NotFoundException.ForNote(request.NoteId);

        // Soft delete: flip the flag and persist. The row stays; the query filter
        // hides it from future reads.
        note.MarkAsDeleted();
        await _repository.UpdateAsync(note, cancellationToken);

        return Unit.Value;
    }
}
