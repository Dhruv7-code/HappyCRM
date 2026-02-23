import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ command }) => ({
  plugins: [
    react(),
    tailwindcss(),
  ],

  // Proxy is only active during `vite dev` (command === 'serve').
  // In production builds VITE_API_URL in .env.production points directly
  // to the Railway backend — no proxy needed.
  server: command === 'serve' ? {
    proxy: {
      '/api': {
        target: 'http://localhost:5050',
        changeOrigin: true,
      },
    },
  } : {},
}))
