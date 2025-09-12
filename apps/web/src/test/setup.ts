import { vi, beforeAll, afterAll, afterEach } from 'vitest'
import '@testing-library/jest-dom'
import { setupServer } from 'msw/node'
import { handlers } from './mocks/handlers'

// Mock global fetch if not available
if (typeof global.fetch === 'undefined') {
  global.fetch = vi.fn()
}

// Filter out Mantine CSS warnings in tests
const originalError = console.error
const originalWarn = console.warn

console.error = (...args) => {
  const message = args.join(' ')
  // Filter out Mantine CSS warnings that clutter test output
  if (
    message.includes('Unsupported style property') ||
    message.includes('Did you mean') ||
    message.includes('mantine-') ||
    message.includes('@media')
  ) {
    return
  }
  originalError.apply(console, args)
}

console.warn = (...args) => {
  const message = args.join(' ')
  // Filter out Mantine warnings that aren't test failures
  if (
    message.includes('Unsupported style property') ||
    message.includes('mantine-')
  ) {
    return
  }
  originalWarn.apply(console, args)
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
