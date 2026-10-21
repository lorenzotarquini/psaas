import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Aspire sets PORT and BACKEND_URL (see PSAAS.AppHost).
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: Number(process.env.PORT) || 5173,
    strictPort: true,
    proxy: {
      // secure: false accepts the ASP.NET dev certificate, which Node does not trust (dev server only).
      '/api': { target: process.env.BACKEND_URL, changeOrigin: true, secure: false },
    },
  },
})
