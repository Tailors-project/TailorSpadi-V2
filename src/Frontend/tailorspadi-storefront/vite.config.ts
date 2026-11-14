import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ command }) => ({
  plugins: [
    react(),
    {
      name: 'local-business-routes',
      configureServer(server) {
        // Production routes these paths to the business app at the reverse proxy.
        server.middlewares.use((req, res, next) => {
          const url = new URL(req.url ?? '/', `http://${req.headers.host ?? '127.0.0.1:5173'}`);
          if (!/^\/(login|register|forgot-password|reset-password|businesses|onboarding|backoffice)(\/|$)/.test(url.pathname)) {
            next();
            return;
          }
          url.port = '5174';
          res.writeHead(307, { Location: url.href });
          res.end();
        });
      },
    },
  ],
  base: command === 'build' ? '/storefront-assets/' : '/',
  server: {
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: false },
    },
  },
}));
