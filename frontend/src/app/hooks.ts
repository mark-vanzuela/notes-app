import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from './store';

/**
 * Pre-typed versions of the react-redux hooks. Using these instead of the raw
 * `useDispatch`/`useSelector` everywhere means:
 *   - useAppSelector knows the shape of RootState (autocomplete on state.*), and
 *   - useAppDispatch knows about our async thunks (so dispatch(thunk()) is typed).
 *
 * This is the officially recommended pattern in the Redux Toolkit + TS docs.
 */
export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector = useSelector.withTypes<RootState>();
