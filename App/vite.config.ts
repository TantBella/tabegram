import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/auth': 'http://localhost:5000',
      '/posts': 'http://localhost:5000',
      '/users': 'http://localhost:5000',
      '/uploads': 'http://localhost:5000'
    }
  }
})
