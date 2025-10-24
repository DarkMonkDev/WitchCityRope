/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // Exclude Playwright E2E tests (they have their own runner)
    exclude: [
      '**/node_modules/**',
      '**/dist/**',
      '**/cypress/**',
      '**/.{idea,git,cache,output,temp}/**',
      '**/{karma,rollup,webpack,vite,vitest,jest,ava,babel,nyc,cypress,tsup,build}.config.*',
      '**/tests/playwright/**',  // Playwright E2E tests
      '**/*.spec.ts',            // All Playwright test files (.spec.ts)
      '**/*.e2e.spec.ts',        // Any E2E test files
    ],
    // Memory management optimized for performance while preventing crashes
    pool: 'forks',  // Use forks instead of threads for better isolation
    poolOptions: {
      forks: {
        singleFork: false,  // Allow parallel test files
        maxForks: 4,        // Limit to 4 concurrent test files
        minForks: 1,
      }
    },
    // Isolation settings for better cleanup
    isolate: true,
    teardownTimeout: 10000,
    maxConcurrency: 5,  // Allow 5 concurrent tests within a file
    // Test timeouts - MAXIMUM 90 seconds enforced
    // WHY 90 SECONDS: No unit/integration test should need more than 60 seconds,
    // but we set max to 90 seconds (1.5 minutes) as a safety buffer.
    // Tests taking >30 seconds indicate slow tests that should be optimized.
    testTimeout: 90000, // 90 seconds ABSOLUTE MAXIMUM per test
    hookTimeout: 30000, // 30 seconds for setup/teardown hooks
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@components': path.resolve(__dirname, './src/components'),
      '@pages': path.resolve(__dirname, './src/pages'),
      '@hooks': path.resolve(__dirname, './src/hooks'),
      '@utils': path.resolve(__dirname, './src/utils'),
      '@services': path.resolve(__dirname, './src/services'),
      '@types': path.resolve(__dirname, './src/types'),
    },
  },
})