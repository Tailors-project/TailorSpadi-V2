import { createContext, useCallback, useContext, useEffect, useMemo, useState, useSyncExternalStore, type ReactNode } from 'react';
import { createApiClient, type ApiClient } from './http';
import { createSessionStore, type SessionStore } from './session';
import type { SessionInfo, SessionResponse } from './types';
import { useQueryClient } from '@tanstack/react-query';

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';

export interface AuthPaths {
  refresh: string;
  login: string;
  logout: string;
}

interface AuthContextValue {
  status: AuthStatus;
  session: SessionInfo | null;
  api: ApiClient['http'];
  login(body: { email: string; password: string }, path?: string): Promise<SessionInfo>;
  applyResponse(response: SessionResponse): void;
  logout(): Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ paths, children }: { paths: AuthPaths; children: ReactNode }) {
  const queryClient = useQueryClient();
  const [store] = useState<SessionStore>(() => createSessionStore());
  const [client] = useState<ApiClient>(() => createApiClient({ store, refreshPath: paths.refresh }));
  const [restored, setRestored] = useState(false);
  const state = useSyncExternalStore(store.subscribe, store.get);

  useEffect(() => {
    let active = true;
    void client.refresh().finally(() => {
      if (active) {
        setRestored(true);
      }
    });
    return () => {
      active = false;
    };
  }, [client]);

  const login = useCallback(
    async (body: { email: string; password: string }, path = paths.login) => {
      const response = await client.http.post<SessionResponse>(path, body);
      store.set(response.data.accessToken, response.data.session);
      return response.data.session;
    },
    [client, paths.login, store],
  );

  const logout = useCallback(async () => {
    try {
      await client.http.post(paths.logout, undefined, { headers: { 'X-Requested-With': 'TailorSpadi' } });
    } finally {
      store.clear();
      queryClient.clear();
    }
  }, [client, paths.logout, queryClient, store]);

  const value = useMemo<AuthContextValue>(
    () => ({
      status: !restored ? 'loading' : state.session ? 'authenticated' : 'anonymous',
      session: state.session,
      api: client.http,
      login,
      logout,
      applyResponse: (response) => store.set(response.accessToken, response.session),
    }),
    [client.http, login, logout, restored, state.session, store],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }
  return context;
}
