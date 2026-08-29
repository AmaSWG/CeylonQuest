import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        // DEV SHORTCUT: proxying directly to the Identity Service,
        // bypassing the API Gateway (localhost:5000).
        // Switch back to 'http://localhost:5000' when testing with the gateway.
        target: 'http://localhost:5278',
        changeOrigin: true,
        secure: false,
      },
      '/uploads': {
        target: 'http://localhost:5278',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

