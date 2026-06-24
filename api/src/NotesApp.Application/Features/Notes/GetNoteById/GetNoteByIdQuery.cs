using MediatR;
using NotesApp.Application.Notes;

namespace NotesApp.Application.Features.Notes.GetNoteById;

/// <summary>Fetch a single note the user owns. 404 if missing/not theirs/deleted.</summary>
public record GetNoteByIdQuery(Guid UserId, Guid NoteId) : IRequest<NoteDto>;
