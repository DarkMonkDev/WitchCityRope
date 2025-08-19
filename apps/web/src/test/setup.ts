import { vi, beforeAll, afterAll, afterEach } from 'vitest'
import '@testing-library/jest-dom'
import { setupServer } from 'msw/node'
import { handlers } from './mocks/handlers'

// Mock global fetch if not available
if (typeof global.fetch === 'undefined') {
  global.fetch = vi.fn()
}

// Mock window.matchMedia for Mantine components
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock ResizeObserver for Mantine components
global.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}))

export const server = setupServer(...handlers)

beforeAll(() => {
  // Use 'warn' instead of 'error' to allow unhandled requests but still see them
  server.listen({ onUnhandledRequest: 'warn' })
})

afterAll(() => {
  server.close()
})

afterEach(() => {
  server.resetHandlers()
})
