import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { renderWithProviders } from '../../test/test-utils';
import { notesApi } from '../../api/notesApi';
import { NotesList } from './NotesList';
import type { Note } from '../../types/note';

// Mock the API module so the component renders against fake data, not real HTTP.
vi.mock('../../api/notesApi', () => ({
  notesApi: { getAll: vi.fn(), remove: vi.fn() },
}));

const notes: Note[] = [
  { id: 'n1', title: 'Groceries', content: 'Milk and eggs', createdAtUtc: '', updatedAtUtc: '' },
];

beforeEach(() => {
  vi.clearAllMocks();
});

describe('<NotesList />', () => {
  it('renders the notes returned by the API', async () => {
    vi.mocked(notesApi.getAll).mockResolvedValue(notes);

    renderWithProviders(<NotesList />);

    // findBy* waits for the async fetch + re-render before asserting.
    expect(await screen.findByText('Groceries')).toBeInTheDocument();
    expect(screen.getByText('Milk and eggs')).toBeInTheDocument();
  });

  it('deletes a note when the user confirms', async () => {
    vi.mocked(notesApi.getAll).mockResolvedValue(notes);
    vi.mocked(notesApi.remove).mockResolvedValue(undefined);
    // The component asks for confirmation via window.confirm — force "OK".
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    renderWithProviders(<NotesList />);
    await screen.findByText('Groceries');

    await userEvent.click(screen.getByRole('button', { name: /delete/i }));

    await waitFor(() => expect(notesApi.remove).toHaveBeenCalledWith('n1'));
  });
});
