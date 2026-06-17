import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev server proxies /api → pimly backend (:8080) so the browser stays
// same-origin (no CORS needed). Client API base defaults to "/api".
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.PIMLY_API_TARGET || 'http://localhost:8080',
        changeOrigin: true,
        rewrite: (p) => p.replace(/^\/api/, ''),
      },
    },
  },
})
