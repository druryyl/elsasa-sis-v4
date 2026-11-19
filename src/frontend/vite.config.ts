import { defineConfig, type UserConfig } from 'vite';
import type { InlineConfig } from 'vitest/node';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const apiProxyTarget = process.env.VITE_API_PROXY_TARGET || 'http://localhost:52931';

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src')
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: apiProxyTarget,
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    target: 'es2022',
    sourcemap: false,
    chunkSizeWarningLimit: 600,
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor-core': ['vue', 'vue-router', 'pinia'],
          'vendor-query': ['@tanstack/vue-query', 'axios'],
          'vendor-primevue': ['primevue'],
          'vendor-utils': ['date-fns', 'zod', 'vee-validate', 'lucide-vue-next']
        }
      }
    }
  },
  test: {
    environment: 'jsdom',
    globals: true,
    include: ['src/**/*.spec.ts', 'src/**/*.test.ts']
  }
} as UserConfig & { test: InlineConfig });
