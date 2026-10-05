import axios from 'axios';

// Vite in development and the gateway in Docker forward same-origin /api.
export const apiClient = axios.create({
  baseURL: '/api',
  timeout: 10_000,
});
