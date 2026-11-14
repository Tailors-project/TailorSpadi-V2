import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import type { SessionStore } from './session';
import type { SessionResponse } from './types';

export interface ApiProblem {
  status: number;
  code: string | null;
  title: string;
  errors: Record<string, string[]>;
}

export function toProblem(error: unknown): ApiProblem {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as Partial<ApiProblem> | undefined;
    return {
      status: error.response?.status ?? 0,
      code: typeof data?.code === 'string' ? data.code : null,
      title: typeof data?.title === 'string' ? data.title : error.response ? 'Something went wrong.' : 'We could not reach the server.',
      errors: data?.errors && typeof data.errors === 'object' ? data.errors : {},
    };
  }
  return { status: 0, code: null, title: 'Something went wrong.', errors: {} };
}

export interface ApiClientOptions {
  store: SessionStore;
  /** Cookie-authenticated refresh endpoint, relative to /api (e.g. `/auth/refresh`). */
  refreshPath: string;
  baseURL?: string;
  adapter?: InternalAxiosRequestConfig['adapter'];
}

export interface ApiClient {
  http: AxiosInstance;
  refresh(): Promise<SessionResponse | null>;
}

const CSRF_HEADERS = { 'X-Requested-With': 'TailorSpadi' };

export function createApiClient({ store, refreshPath, baseURL = '/api', adapter }: ApiClientOptions): ApiClient {
  const http = axios.create({ baseURL, timeout: 15_000, withCredentials: true, adapter });
  const bare = axios.create({ baseURL, timeout: 15_000, withCredentials: true, adapter });
  let inflight: Promise<SessionResponse | null> | null = null;

  /** All callers share one refresh request, so a rotating token is never used twice. */
  const refresh = (): Promise<SessionResponse | null> => {
    inflight ??= bare
      .post<SessionResponse>(refreshPath, undefined, { headers: CSRF_HEADERS })
      .then((response) => {
        store.set(response.data.accessToken, response.data.session);
        return response.data;
      })
      .catch(() => {
        store.clear();
        return null;
      })
      .finally(() => {
        inflight = null;
      });
    return inflight;
  };

  http.interceptors.request.use((config) => {
    const token = store.get().accessToken;
    if (token && !config.headers.has('Authorization')) {
      config.headers.set('Authorization', `Bearer ${token}`);
    }
    return config;
  });

  http.interceptors.response.use(undefined, async (error: AxiosError) => {
    const config = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;
    const sentToken = config?.headers?.has('Authorization') ?? false;
    if (error.response?.status === 401 && config && !config._retried && sentToken) {
      config._retried = true;
      const renewed = await refresh();
      if (renewed) {
        config.headers.set('Authorization', `Bearer ${renewed.accessToken}`);
        return http.request(config);
      }
    }
    throw error;
  });

  return { http, refresh };
}
