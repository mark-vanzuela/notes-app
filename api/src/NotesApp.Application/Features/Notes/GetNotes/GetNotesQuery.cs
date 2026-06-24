using MediatR;
using NotesApp.Application.Notes;

namespace NotesApp.Application.Features.Notes.GetNotes;

/// <summary>CQRS QUERY: list all of the user's notes (read-only, changes nothing).</summary>
public record GetNotesQuery(Guid UserId) : IRequest<IReadOnlyList<NoteDto>>;
