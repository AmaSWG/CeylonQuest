import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],

  server: {
    proxy: {
      // Provider Catalog Service
      '/api/catalog': {
        target: 'http://localhost:5141',
        changeOrigin: true,
        secure: false,
      },

      // Booking Service
      '/api/Bookings': {
        target: 'http://localhost:5229',
        changeOrigin: true,
        secure: false,
      },

      // Identity Service
      '/api': {
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