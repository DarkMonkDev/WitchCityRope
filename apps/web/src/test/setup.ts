import { vi } from 'vitest'
import '@testing-library/jest-dom'

// Mock global fetch if not available
if (typeof global.fetch === 'undefined') {
  global.fetch = vi.fn()
}
