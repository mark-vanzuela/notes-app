namespace NotesApp.Api.Contracts;

/// <summary>JSON body for POST /api/notes. The owner is taken from the JWT, not the body.</summary>
public record CreateNoteRequest(string Title, string Content);

/// <summary>JSON body for PUT /api/notes/{id}. The id comes from the URL.</summary>
public record UpdateNoteRequest(string Title, string Content);
