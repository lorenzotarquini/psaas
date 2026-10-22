import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import packageJson from './package.json' with { type: 'json' }

// Aspire sets PORT and BACKEND_URL (see PSAAS.AppHost).
export default defineConfig({
  plugins: [react(), tailwindcss()],
  define: { __APP_VERSION__: JSON.stringify(packageJson.version) },
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port: Number(process.env.PORT) || 5173,
    strictPort: true,
    proxy: {
      // secure: false accepts the ASP.NET dev certificate, which Node does not trust (dev server only).
      '/api': { target: process.env.BACKEND_URL, changeOrigin: true, secure: false },
    },
  },
})
