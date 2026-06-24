using MediatR;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Notes;

namespace NotesApp.Application.Features.Notes.GetNotes;

public class GetNotesHandler : IRequestHandler<GetNotesQuery, IReadOnlyList<NoteDto>>
{
    private readonly INoteRepository _repository;

    public GetNotesHandler(INoteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<NoteDto>> Handle(GetNotesQuery request, CancellationToken cancellationToken)
    {
        var notes = await _repository.GetAllAsync(request.UserId, cancellationToken);
        return notes.Select(n => n.ToDto()).ToList();
    }
}
