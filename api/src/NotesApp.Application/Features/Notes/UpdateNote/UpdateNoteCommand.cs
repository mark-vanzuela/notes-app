using MediatR;
using NotesApp.Application.Notes;

namespace NotesApp.Application.Features.Notes.UpdateNote;

/// <summary>Edits an existing note the user owns. <see cref="NoteId"/> comes from the URL.</summary>
public record UpdateNoteCommand(Guid UserId, Guid NoteId, string Title, string Content) : IRequest<NoteDto>;
