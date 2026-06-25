import { setupStore } from '../../app/store';
import { notesApi } from '../../api/notesApi';
import {
  fetchNotes,
  deleteNote,
  clearSelected,
  selectNotes,
  selectNotesStatus,
} from './notesSlice';
import type { Note } from '../../types/note';

vi.mock('../../api/notesApi', () => ({
  notesApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
  },
}));

const notes: Note[] = [
  { id: 'n1', title: 'First', content: 'A', createdAtUtc: '', updatedAtUtc: '' },
  { id: 'n2', title: 'Second', content: 'B', createdAtUtc: '', updatedAtUtc: '' },
];

beforeEach(() => {
  vi.clearAllMocks();
});

describe('notesSlice', () => {
  it('loads notes (fetchNotes fulfilled -> succeeded + items)', async () => {
    vi.mocked(notesApi.getAll).mockResolvedValue(notes);
    const store = setupStore();

    await store.dispatch(fetchNotes());

    expect(selectNotesStatus(store.getState())).toBe('succeeded');
    expect(selectNotes(store.getState())).toHaveLength(2);
  });

  it('sets failed status + error when the load throws', async () => {
    vi.mocked(notesApi.getAll).mockRejectedValue(new Error('boom'));
    const store = setupStore();

    await store.dispatch(fetchNotes());

    expect(selectNotesStatus(store.getState())).toBe('failed');
    expect(store.getState().notes.error).toBeTruthy();
  });

  it('drops the deleted note from the list', async () => {
    vi.mocked(notesApi.getAll).mockResolvedValue(notes);
    vi.mocked(notesApi.remove).mockResolvedValue(undefined);
    const store = setupStore();
    await store.dispatch(fetchNotes());

    await store.dispatch(deleteNote('n1'));

    const remaining = selectNotes(store.getState());
    expect(remaining).toHaveLength(1);
    expect(remaining[0].id).toBe('n2');
  });

  it('clearSelected resets the selected note', () => {
    const store = setupStore();
    store.dispatch(clearSelected());
    expect(store.getState().notes.selected).toBeNull();
  });
});
