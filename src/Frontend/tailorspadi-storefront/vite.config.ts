import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ command }) => ({
  plugins: [react()],
  base: command === 'build' ? '/storefront-assets/' : '/',
  server: {
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: false },
    },
  },
}));
