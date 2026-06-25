import type { Note, NotePayload } from '../types/note';
import { request } from './client';

// All note HTTP calls in one object. The JWT is attached automatically by the
// shared `request` helper, so these stay focused on routes + payloads.
export const notesApi = {
  getAll: () => request<Note[]>('/notes'),

  getById: (id: string) => request<Note>(`/notes/${id}`),

  create: (payload: NotePayload) =>
    request<Note>('/notes', { method: 'POST', body: JSON.stringify(payload) }),

  update: (id: string, payload: NotePayload) =>
    request<Note>(`/notes/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),

  remove: (id: string) => request<void>(`/notes/${id}`, { method: 'DELETE' }),
};
