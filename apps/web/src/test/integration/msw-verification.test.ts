import { describe, it, expect } from 'vitest'
import { api } from '../../api/client'

/**
 * MSW Verification Tests
 *
 * These tests verify that MSW is properly intercepting requests
 * and returning the expected response structure.
 *
 * STANDARD AUTHENTICATION PATTERN - WitchCityRope Project
 * Pattern: TanStack Query Mutations + Zustand Store
 * See: /docs/standards-processes/frontend/authentication-pattern-guide.md
 */
describe('MSW Request Interception', () => {
  it('should intercept login requests with correct response structure', async () => {
    const credentials = {
      email: 'admin@witchcityrope.com',
      emailOrSceneName: 'admin@witchcityrope.com',
      password: 'Test123!',
    }

    const response = await api.post('/api/auth/login', credentials)

    // Verify nested response structure matches API expectations
    expect(response.data).toHaveProperty('user')
    expect(response.data.user).toEqual({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      firstName: null,
      lastName: null,
      roles: ['Admin'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: '2025-08-19T10:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    })
  })

  it('should intercept logout requests', async () => {
    // Should not throw any errors
    await expect(api.post('/api/auth/logout')).resolves.toBeDefined()
  })

  it('should intercept protected welcome requests', async () => {
    // With httpOnly cookies, authentication is automatic
    const response = await api.get('/api/protected/welcome')

    expect(response.data).toHaveProperty('message')
    expect(response.data).toHaveProperty('user')
    expect(response.data).toHaveProperty('serverTime')
    expect(response.data.message).toBe('Welcome to the protected area!')
    expect(response.data.user).toEqual({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      firstName: null,
      lastName: null,
      roles: ['Admin'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: '2025-08-19T10:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    })
  })

  it('should handle unauthorized requests', async () => {
    const credentials = {
      email: 'invalid@example.com',
      emailOrSceneName: 'invalid@example.com',
      password: 'wrongpassword',
    }

    await expect(api.post('/api/auth/login', credentials)).rejects.toThrow()
  })
})