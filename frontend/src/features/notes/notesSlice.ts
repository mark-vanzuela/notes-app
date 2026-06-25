import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

import { notesApi } from '../../api/notesApi';
import type { Note, NotePayload } from '../../types/note';
import type { RootState } from '../../app/store';

/*
  ============================================================================
  NOTES SLICE — owns the current user's notes.
  ============================================================================
  Same shape as the reference customersSlice: a list + status for the list view,
  and a `selected` note + status for the detail/edit view. Async work goes through
  createAsyncThunk, which dispatches pending/fulfilled/rejected we handle below.
*/

interface NotesState {
  items: Note[];
  status: 'idle' | 'loading' | 'succeeded' | 'failed';
  error: string | null;
  selected: Note | null;
  selectedStatus: 'idle' | 'loading' | 'succeeded' | 'failed';
}

const initialState: NotesState = {
  items: [],
  status: 'idle',
  error: null,
  selected: null,
  selectedStatus: 'idle',
};

// ---- Async thunks ----
export const fetchNotes = createAsyncThunk('notes/fetchAll', () => notesApi.getAll());

export const fetchNoteById = createAsyncThunk('notes/fetchById', (id: string) =>
  notesApi.getById(id)
);

export const createNote = createAsyncThunk('notes/create', (payload: NotePayload) =>
  notesApi.create(payload)
);

export const updateNote = createAsyncThunk(
  'notes/update',
  ({ id, payload }: { id: string; payload: NotePayload }) => notesApi.update(id, payload)
);

export const deleteNote = createAsyncThunk('notes/delete', async (id: string) => {
  await notesApi.remove(id);
  // Return the id so the reducer knows which item to drop from the list.
  return id;
});

const notesSlice = createSlice({
  name: 'notes',
  initialState,
  reducers: {
    // Clear the selected note when leaving the form, so stale data doesn't flash.
    clearSelected(state) {
      state.selected = null;
      state.selectedStatus = 'idle';
    },
  },
  extraReducers: (builder) => {
    builder
      // ---- list ----
      .addCase(fetchNotes.pending, (state) => {
        state.status = 'loading';
        state.error = null;
      })
      .addCase(fetchNotes.fulfilled, (state, action: PayloadAction<Note[]>) => {
        state.status = 'succeeded';
        state.items = action.payload;
      })
      .addCase(fetchNotes.rejected, (state) => {
        state.status = 'failed';
        state.error = 'Could not load notes. Is the API running?';
      })

      // ---- single ----
      .addCase(fetchNoteById.pending, (state) => {
        state.selectedStatus = 'loading';
        state.selected = null;
      })
      .addCase(fetchNoteById.fulfilled, (state, action: PayloadAction<Note>) => {
        state.selectedStatus = 'succeeded';
        state.selected = action.payload;
      })
      .addCase(fetchNoteById.rejected, (state) => {
        state.selectedStatus = 'failed';
      })

      // ---- delete: drop the item from the in-memory list immediately ----
      .addCase(deleteNote.fulfilled, (state, action: PayloadAction<string>) => {
        state.items = state.items.filter((n) => n.id !== action.payload);
      });
    // create/update navigate back to the list, which re-fetches, so they need no
    // special handling here.
  },
});

export const { clearSelected } = notesSlice.actions;

// ---- Selectors ----
export const selectNotes = (state: RootState) => state.notes.items;
export const selectNotesStatus = (state: RootState) => state.notes.status;
export const selectNotesError = (state: RootState) => state.notes.error;
export const selectSelectedNote = (state: RootState) => state.notes.selected;
export const selectSelectedStatus = (state: RootState) => state.notes.selectedStatus;

export default notesSlice.reducer;
