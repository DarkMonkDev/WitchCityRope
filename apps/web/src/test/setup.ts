import { vi, beforeAll, afterAll, afterEach } from 'vitest'
import '@testing-library/jest-dom'
import { setupServer } from 'msw/node'
import { handlers } from './mocks/handlers'

// Mock global fetch if not available
if (typeof global.fetch === 'undefined') {
  global.fetch = vi.fn()
}

export const server = setupServer(...handlers)

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' })
})

afterAll(() => {
  server.close()
})

afterEach(() => {
  server.resetHandlers()
})
