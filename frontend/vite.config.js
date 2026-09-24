import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

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

      // Booking Service - Unified visitor bookings/reservations
      '/api/user-bookings': {
        target: 'http://localhost:5229',
        changeOrigin: true,
        secure: false,
      },

      '/api/provider-bookings': {
        target: 'http://localhost:5229',
        changeOrigin: true,
        secure: false,
      },

      // Booking Service - Experience bookings
      '/api/Bookings': {
        target: 'http://localhost:5229',
        changeOrigin: true,
        secure: false,
      },

      // Booking Service - Restaurant reservations
      '/api/Reservations': {
        target: 'http://localhost:5229',
        changeOrigin: true,
        secure: false,
      },

      // Identity Service
      // Keep this AFTER the more specific service routes.
      '/api': {
        target: 'http://localhost:5278',
        changeOrigin: true,
        secure: false,
      },

      // Uploaded files
      '/uploads': {
        target: 'http://localhost:5278',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})