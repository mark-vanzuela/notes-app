using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApp.Api.Contracts;
using NotesApp.Application.Abstractions;
using NotesApp.Application.Features.Notes.CreateNote;
using NotesApp.Application.Features.Notes.DeleteNote;
using NotesApp.Application.Features.Notes.GetNoteById;
using NotesApp.Application.Features.Notes.GetNotes;
using NotesApp.Application.Features.Notes.UpdateNote;
using NotesApp.Application.Notes;

namespace NotesApp.Api.Controllers;

/// <summary>
/// CRUD for the signed-in user's notes. Thin by design: each action translates HTTP
/// into a MediatR message and back. [Authorize] means a valid JWT is required —
/// requests without one get 401 automatically.
///
/// Crucially, the owner is taken from <see cref="ICurrentUser"/> (the JWT), never
/// from the request body, so a caller can only ever touch their own notes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public NotesController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>GET /api/notes — list the current user's notes.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NoteDto>>> GetAll(CancellationToken ct)
    {
        var notes = await _sender.Send(new GetNotesQuery(_currentUser.Id), ct);
        return Ok(notes);
    }

    /// <summary>GET /api/notes/{id} — one of the current user's notes.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteDto>> GetById(Guid id, CancellationToken ct)
    {
        var note = await _sender.Send(new GetNoteByIdQuery(_currentUser.Id, id), ct);
        return Ok(note);
    }

    /// <summary>POST /api/notes — create.</summary>
    [HttpPost]
    public async Task<ActionResult<NoteDto>> Create([FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        var command = new CreateNoteCommand(_currentUser.Id, request.Title, request.Content);
        var created = await _sender.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/notes/{id} — update.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NoteDto>> Update(Guid id, [FromBody] UpdateNoteRequest request, CancellationToken ct)
    {
        var command = new UpdateNoteCommand(_currentUser.Id, id, request.Title, request.Content);
        var updated = await _sender.Send(command, ct);
        return Ok(updated);
    }

    /// <summary>DELETE /api/notes/{id} — soft delete.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteNoteCommand(_currentUser.Id, id), ct);
        return NoContent();
    }
}
