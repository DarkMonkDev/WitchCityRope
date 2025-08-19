import { describe, it, expect } from 'vitest'
import { authService } from '../../services/authService'

/**
 * MSW Verification Tests
 * 
 * These tests verify that MSW is properly intercepting requests
 * and returning the expected response structure.
 */
describe('MSW Request Interception', () => {
  it('should intercept login requests with correct response structure', async () => {
    const credentials = {
      email: 'admin@witchcityrope.com',
      password: 'Test123!'
    }

    const response = await authService.login(credentials)

    // Verify nested response structure matches API expectations
    expect(response).toHaveProperty('user')
    expect(response).toHaveProperty('token')
    expect(response.user).toEqual({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      createdAt: '2025-08-19T00:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    })
    expect(response.token).toBe('fake-jwt-token')
  })

  it('should intercept logout requests', async () => {
    // Should not throw any errors
    await expect(authService.logout()).resolves.toBeUndefined()
  })

  it('should intercept protected welcome requests', async () => {
    // Set a token first
    authService.setToken('fake-jwt-token')

    const response = await authService.getProtectedWelcome()

    expect(response).toHaveProperty('message')
    expect(response).toHaveProperty('user')
    expect(response).toHaveProperty('serverTime')
    expect(response.message).toBe('Welcome to the protected area!')
    expect(response.user).toEqual({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      createdAt: '2025-08-19T00:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    })
  })

  it('should handle unauthorized requests', async () => {
    const credentials = {
      email: 'invalid@example.com',
      password: 'wrongpassword'
    }

    await expect(authService.login(credentials)).rejects.toThrow()
  })
})