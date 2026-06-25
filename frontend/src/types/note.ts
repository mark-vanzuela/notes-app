// TypeScript types describe the SHAPE of data at compile time only — they vanish
// at runtime. This Note mirrors the NoteDto the .NET API returns.
export interface Note {
  id: string;
  title: string;
  content: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

// What we SEND when creating/updating. The server owns id/timestamps/ownership,
// so we never include those.
export interface NotePayload {
  title: string;
  content: string;
}
