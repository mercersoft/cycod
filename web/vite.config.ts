import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { copyFileSync } from 'node:fs'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    {
      name: 'copy-staticwebapp-config',
      closeBundle() {
        try {
          copyFileSync('public/staticwebapp.config.json', 'dist/staticwebapp.config.json')
        } catch (err) {
          console.warn('Could not copy staticwebapp.config.json:', err)
        }
      }
    }
  ],
  resolve: {
    alias: {
      "@": resolve(fileURLToPath(new URL('.', import.meta.url)), "./src"),
    },
  },
  build: {
    // Optimize for production size
    minify: 'esbuild',
    cssMinify: true,
    rollupOptions: {
      output: {
        // Optimize chunk splitting for better caching
        manualChunks: {
          vendor: ['react', 'react-dom'],
          ui: ['@radix-ui/react-slot', 'lucide-react']
        },
        // Exclude source maps to reduce size
        sourcemap: false
      }
    },
    // Target modern browsers for smaller output
    target: 'es2020'
  },
  // Optimize public directory handling
  publicDir: 'public',
  assetsInclude: ['**/*.wasm']
})
