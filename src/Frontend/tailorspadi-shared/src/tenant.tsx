import { createContext, useContext, useEffect, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import axios from 'axios';
import type { TenantBranding } from './types';

type TenantState =
  | { kind: 'loading' }
  | { kind: 'ready'; tenant: TenantBranding }
  | { kind: 'not-found' }
  | { kind: 'unavailable' }
  | { kind: 'error'; retry(): void };

const TenantContext = createContext<TenantState>({ kind: 'loading' });

export function useTenantState(): TenantState {
  return useContext(TenantContext);
}

export function useTenant(): TenantBranding {
  const state = useContext(TenantContext);
  if (state.kind !== 'ready') {
    throw new Error('useTenant requires a resolved tenant');
  }
  return state.tenant;
}

/** Loads the branding of the tenant for the current host and applies its colours as CSS variables. */
export function TenantProvider({ children }: { children: ReactNode }) {
  const query = useQuery({
    queryKey: ['tenant-current'],
    retry: false,
    queryFn: async ({ signal }) => (await axios.get<TenantBranding>('/api/tenant/current', { signal })).data,
  });

  useEffect(() => {
    const tenant = query.data;
    if (tenant) {
      document.documentElement.style.setProperty('--tenant-primary', tenant.primaryColor);
      document.documentElement.style.setProperty('--tenant-secondary', tenant.secondaryColor);
      document.title = tenant.name;
    }
  }, [query.data]);

  let state: TenantState = { kind: 'loading' };
  if (query.data) {
    state = { kind: 'ready', tenant: query.data };
  } else if (query.isError) {
    const status = axios.isAxiosError(query.error) ? query.error.response?.status : undefined;
    state = status === 404 ? { kind: 'not-found' } : status === 403 ? { kind: 'unavailable' } : { kind: 'error', retry: () => void query.refetch() };
  }
  return <TenantContext.Provider value={state}>{children}</TenantContext.Provider>;
}

export function TenantGate({ children }: { children: ReactNode }) {
  const state = useTenantState();
  switch (state.kind) {
    case 'loading':
      return <p role="status" className="text-secondary">Loading...</p>;
    case 'not-found':
      return (
        <section role="alert">
          <h1>Store not found</h1>
          <p>We could not find a store at this address. Check the link and try again.</p>
        </section>
      );
    case 'unavailable':
      return (
        <section role="alert">
          <h1>Store unavailable</h1>
          <p>This store is temporarily unavailable. Please check back later.</p>
        </section>
      );
    case 'error':
      return (
        <section role="alert">
          <h1>We could not load this store</h1>
          <button className="btn btn-outline-secondary" onClick={state.retry}>Try again</button>
        </section>
      );
    default:
      return <>{children}</>;
  }
}
