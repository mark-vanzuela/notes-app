import { useEffect } from 'react';
import { Link } from 'react-router-dom';

import { useAppDispatch, useAppSelector } from '../../app/hooks';
import {
  fetchNotes,
  deleteNote,
  selectNotes,
  selectNotesStatus,
  selectNotesError,
} from './notesSlice';
import type { Note } from '../../types/note';
import styles from './NotesList.module.css';

/**
 * Shows the user's notes as a responsive grid of cards. Demonstrates the full
 * Redux read/write loop:
 *   - READ state with useAppSelector(selector)
 *   - WRITE by dispatching a thunk: dispatch(fetchNotes())
 */
export function NotesList() {
  const dispatch = useAppDispatch();

  const notes = useAppSelector(selectNotes);
  const status = useAppSelector(selectNotesStatus);
  const error = useAppSelector(selectNotesError);

  // Load notes once when the component mounts.
  useEffect(() => {
    dispatch(fetchNotes());
  }, [dispatch]);

  function handleDelete(note: Note) {
    const ok = confirm(`Delete "${note.title}"? This is a soft delete.`);
    if (!ok) return;
    dispatch(deleteNote(note.id));
  }

  return (
    <section>
      <header className={styles.header}>
        <h1>Your notes</h1>
        <Link className="btn btn--primary" to="/notes/new">
          + New note
        </Link>
      </header>

      {status === 'loading' && <p className="muted">Loading…</p>}

      {status === 'failed' && <p className="alert alert--error">{error}</p>}

      {status === 'succeeded' && notes.length === 0 && (
        <div className={styles.empty}>
          <p>No notes yet — your first one is a click away.</p>
        </div>
      )}

      {status === 'succeeded' && notes.length > 0 && (
        <div className={styles.grid}>
          {notes.map((note) => (
            <article key={note.id} className={styles.card}>
              <h2 className={styles.title}>{note.title}</h2>
              <p className={styles.preview}>{note.content}</p>
              <div className={styles.actions}>
                <Link className="btn btn--ghost" to={`/notes/${note.id}/edit`}>
                  Edit
                </Link>
                <button className="btn btn--danger" onClick={() => handleDelete(note)}>
                  Delete
                </button>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
