using MediatR;

namespace NotesApp.Application.Features.Notes.DeleteNote;

/// <summary>Soft-deletes a note the user owns. Returns nothing (Unit) on success.</summary>
public record DeleteNoteCommand(Guid UserId, Guid NoteId) : IRequest<Unit>;
