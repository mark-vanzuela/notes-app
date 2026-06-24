using MediatR;
using NotesApp.Application.Notes;

namespace NotesApp.Application.Features.Notes.CreateNote;

/// <summary>
/// CQRS COMMAND: creates a note for a user. <see cref="UserId"/> is supplied by the
/// API from the authenticated principal — never from the request body — so a caller
/// cannot create notes on behalf of someone else.
/// </summary>
public record CreateNoteCommand(Guid UserId, string Title, string Content) : IRequest<NoteDto>;
