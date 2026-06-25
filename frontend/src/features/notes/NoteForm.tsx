import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { useAppDispatch, useAppSelector } from '../../app/hooks';
import {
  createNote,
  updateNote,
  fetchNoteById,
  clearSelected,
  selectSelectedNote,
  selectSelectedStatus,
} from './notesSlice';
import styles from './NoteForm.module.css';

/**
 * One component handles BOTH create (/notes/new) and edit (/notes/:id/edit). The
 * presence of an `:id` route param tells us which mode we're in.
 *
 * Form fields are local component state (useState) — they only matter to this
 * screen, so they don't need to live in Redux.
 */
export function NoteForm() {
  const { id } = useParams();
  const isEdit = Boolean(id);

  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const selected = useAppSelector(selectSelectedNote);
  const selectedStatus = useAppSelector(selectSelectedStatus);

  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [errors, setErrors] = useState<{ title?: string; content?: string }>({});
  const [submitting, setSubmitting] = useState(false);

  // When editing, load the note. The cleanup clears it so the next visit to the
  // form doesn't briefly show stale data.
  useEffect(() => {
    if (isEdit && id) {
      dispatch(fetchNoteById(id));
    }
    return () => {
      dispatch(clearSelected());
    };
  }, [dispatch, isEdit, id]);

  // Copy the loaded note into the editable fields once it arrives.
  useEffect(() => {
    if (isEdit && selected) {
      setTitle(selected.title);
      setContent(selected.content);
    }
  }, [isEdit, selected]);

  function validate(): boolean {
    const next: { title?: string; content?: string } = {};
    if (!title.trim()) next.title = 'Title is required.';
    if (!content.trim()) next.content = 'Content is required.';
    setErrors(next);
    return Object.keys(next).length === 0;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!validate()) return;

    setSubmitting(true);
    const payload = { title: title.trim(), content: content.trim() };
    const result =
      isEdit && id
        ? await dispatch(updateNote({ id, payload }))
        : await dispatch(createNote(payload));
    setSubmitting(false);

    // On success, go back to the list (which re-fetches).
    if (result.meta.requestStatus === 'fulfilled') {
      navigate('/notes');
    }
  }

  if (isEdit && selectedStatus === 'loading') {
    return <p className="muted">Loading…</p>;
  }
  if (isEdit && selectedStatus === 'failed') {
    return <p className="alert alert--error">That note could not be found.</p>;
  }

  return (
    <section className={styles.wrap}>
      <h1>{isEdit ? 'Edit note' : 'New note'}</h1>

      <form onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label htmlFor="title">Title</label>
          <input
            id="title"
            value={title}
            maxLength={200}
            onChange={(e) => setTitle(e.target.value)}
          />
          {errors.title && <p className="field__error">{errors.title}</p>}
        </div>

        <div className="field">
          <label htmlFor="content">Content</label>
          <textarea
            id="content"
            rows={8}
            value={content}
            onChange={(e) => setContent(e.target.value)}
          />
          {errors.content && <p className="field__error">{errors.content}</p>}
        </div>

        <div className={styles.actions}>
          <button type="button" className="btn btn--ghost" onClick={() => navigate('/notes')}>
            Cancel
          </button>
          <button type="submit" className="btn btn--primary" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </section>
  );
}
