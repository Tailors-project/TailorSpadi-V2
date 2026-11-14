import { describe, expect, it, vi } from 'vitest';
import { createSessionStore } from './session';
import type { SessionInfo } from './types';

const info: SessionInfo = {
  scope: 'platform',
  user: { id: '1', email: 'a@b.c', firstName: 'A', lastName: 'B', phone: null },
  role: null, tenant: null, businesses: [], hasDraft: false,
};

describe('session store', () => {
  it('holds the token in memory and notifies subscribers', () => {
    const store = createSessionStore();
    const listener = vi.fn();
    store.subscribe(listener);
    store.set('token', info);
    expect(store.get().accessToken).toBe('token');
    store.clear();
    expect(store.get().session).toBeNull();
    expect(listener).toHaveBeenCalledTimes(2);
  });
});
