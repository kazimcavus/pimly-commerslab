import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev server proxies /api → .NET backend (Pimly.Api, :7000) so the browser stays
// same-origin (no CORS needed). The .NET API serves under /api/v1/<module>, so the
// /api prefix is forwarded as-is (no rewrite).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.PIMLY_API_TARGET || 'http://localhost:7000',
        changeOrigin: true,
      },
    },
  },
})
