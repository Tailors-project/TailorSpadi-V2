import type { AxiosAdapter, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { AxiosError } from 'axios';
import { describe, expect, it } from 'vitest';
import { createApiClient, toProblem } from './http';
import { createSessionStore } from './session';
import type { SessionInfo } from './types';

const info: SessionInfo = {
  scope: 'platform',
  user: { id: '1', email: 'a@b.c', firstName: 'A', lastName: 'B', phone: null },
  role: null, tenant: null, businesses: [], hasDraft: false,
};

function respond(config: InternalAxiosRequestConfig, status: number, data: unknown): Promise<AxiosResponse> {
  const response = { data, status, statusText: '', headers: {}, config } as AxiosResponse;
  if (status >= 400) {
    return Promise.reject(new AxiosError('failed', String(status), config, null, response));
  }
  return Promise.resolve(response);
}

describe('api client', () => {
  it('refreshes once for concurrent 401s and retries each request with the new token', async () => {
    const store = createSessionStore();
    store.set('old', info);
    let refreshCalls = 0;
    const seen: string[] = [];
    const adapter: AxiosAdapter = (config) => {
      if (config.url === '/auth/refresh') {
        refreshCalls++;
        expect(config.headers.get('X-Requested-With')).toBe('TailorSpadi');
        return respond(config, 200, { accessToken: 'new', expiresInSeconds: 900, session: info });
      }
      const auth = String(config.headers.get('Authorization'));
      seen.push(auth);
      return auth === 'Bearer new' ? respond(config, 200, { ok: true }) : respond(config, 401, {});
    };
    const { http } = createApiClient({ store, refreshPath: '/auth/refresh', adapter });

    const results = await Promise.all([http.get('/a'), http.get('/b'), http.get('/c')]);

    expect(results.every((r) => r.status === 200)).toBe(true);
    expect(refreshCalls).toBe(1);
    expect(store.get().accessToken).toBe('new');
  });

  it('clears the session when refresh fails and does not loop', async () => {
    const store = createSessionStore();
    store.set('old', info);
    const adapter: AxiosAdapter = (config) => respond(config, 401, {});
    const { http } = createApiClient({ store, refreshPath: '/auth/refresh', adapter });

    await expect(http.get('/a')).rejects.toBeTruthy();
    expect(store.get().accessToken).toBeNull();
  });

  it('does not try to refresh an unauthenticated request', async () => {
    const store = createSessionStore();
    let calls = 0;
    const adapter: AxiosAdapter = (config) => { calls++; return respond(config, 401, { code: 'invalid_credentials', title: 'Bad' }); };
    const { http } = createApiClient({ store, refreshPath: '/auth/refresh', adapter });

    await expect(http.post('/auth/login', {})).rejects.toBeTruthy();
    expect(calls).toBe(1);
  });

  it('maps problem documents', () => {
    const error = new AxiosError('x', '400', undefined, null, {
      data: { code: 'email_taken', title: 'Taken', errors: { email: ['in use'] } }, status: 409, statusText: '', headers: {}, config: {} as InternalAxiosRequestConfig,
    });
    expect(toProblem(error)).toEqual({ status: 409, code: 'email_taken', title: 'Taken', errors: { email: ['in use'] } });
  });
});
