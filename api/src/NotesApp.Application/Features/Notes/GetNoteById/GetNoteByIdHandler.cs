using MediatR;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Common.Exceptions;
using NotesApp.Application.Notes;

namespace NotesApp.Application.Features.Notes.GetNoteById;

public class GetNoteByIdHandler : IRequestHandler<GetNoteByIdQuery, NoteDto>
{
    private readonly INoteRepository _repository;

    public GetNoteByIdHandler(INoteRepository repository)
    {
        _repository = repository;
    }

    public async Task<NoteDto> Handle(GetNoteByIdQuery request, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByIdAsync(request.UserId, request.NoteId, cancellationToken)
            ?? throw NotFoundException.ForNote(request.NoteId);

        return note.ToDto();
    }
}
