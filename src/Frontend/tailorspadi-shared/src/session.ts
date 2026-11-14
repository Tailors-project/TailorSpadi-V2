import type { SessionInfo } from './types';

/** The access token lives only in memory. Nothing here touches web storage. */
export interface SessionState {
  accessToken: string | null;
  session: SessionInfo | null;
}

type Listener = () => void;

export function createSessionStore() {
  let state: SessionState = { accessToken: null, session: null };
  const listeners = new Set<Listener>();
  const emit = () => listeners.forEach((listener) => listener());

  return {
    get: () => state,
    set(accessToken: string, session: SessionInfo) {
      state = { accessToken, session };
      emit();
    },
    clear() {
      if (state.accessToken === null && state.session === null) {
        return;
      }
      state = { accessToken: null, session: null };
      emit();
    },
    subscribe(listener: Listener) {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
  };
}

export type SessionStore = ReturnType<typeof createSessionStore>;
