import { describe, it, expect, beforeAll } from 'vitest'

/**
 * CMS API Integration Tests
 *
 * Tests API endpoints with real network calls to Docker containers
 * These tests verify the API contract, response formats, and error handling
 *
 * Prerequisites:
 * - Docker containers running (./dev.sh)
 * - API available at http://localhost:5655
 * - Database seeded with initial CMS pages
 */

const API_BASE_URL = 'http://localhost:5655'

describe('CMS API Integration Tests', () => {
  describe('GET /api/cms/pages/{slug} - Public Access', () => {
    it('fetches resources page successfully', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/resources`)

      expect(response.status).toBe(200)

      const data = await response.json()
      expect(data).toHaveProperty('id')
      expect(data).toHaveProperty('slug', 'resources')
      expect(data).toHaveProperty('title')
      expect(data).toHaveProperty('content')
      expect(data).toHaveProperty('updatedAt')
      expect(data).toHaveProperty('lastModifiedBy')
    })

    it('fetches contact-us page successfully', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/contact-us`)

      expect(response.status).toBe(200)

      const data = await response.json()
      expect(data.slug).toBe('contact-us')
      expect(data.title).toBeTruthy()
      expect(data.content).toBeTruthy()
    })

    it('fetches private-lessons page successfully', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/private-lessons`)

      expect(response.status).toBe(200)

      const data = await response.json()
      expect(data.slug).toBe('private-lessons')
      expect(data.title).toBeTruthy()
      expect(data.content).toBeTruthy()
    })

    it('returns 404 for non-existent page', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/nonexistent-page`)

      expect(response.status).toBe(404)
    })

    it('returns sanitized HTML content', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/resources`)
      const data = await response.json()

      // Verify content is HTML
      expect(data.content).toBeTruthy()

      // Verify NO script tags present (sanitized)
      expect(data.content).not.toContain('<script>')
      expect(data.content).not.toContain('javascript:')
      expect(data.content).not.toContain('onerror=')
      expect(data.content).not.toContain('onclick=')
    })
  })

  describe('PUT /api/cms/pages/{id} - Admin Only', () => {
    it('returns 401 for unauthenticated requests', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/1`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: 'Updated Title',
          content: '<p>Updated content</p>',
        }),
      })

      expect(response.status).toBe(401)
    })

    // Note: Testing authenticated requests requires cookie-based auth setup
    // This would require a full authentication flow in the test
    // For now, we verify the endpoint exists and rejects unauthenticated requests
  })

  describe('GET /api/cms/pages - Admin Only', () => {
    it('returns 401 for unauthenticated requests', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages`)

      expect(response.status).toBe(401)
    })
  })

  describe('GET /api/cms/pages/{id}/revisions - Admin Only', () => {
    it('returns 401 for unauthenticated requests', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/1/revisions`)

      expect(response.status).toBe(401)
    })
  })

  describe('Content Sanitization Verification', () => {
    it('API returns safe HTML tags only', async () => {
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/resources`)
      const data = await response.json()

      const content = data.content as string

      // Verify allowed tags are present (safe HTML)
      const allowedTagsPattern = /<(p|h[1-6]|strong|em|ul|ol|li|a|blockquote|code|pre)/
      expect(allowedTagsPattern.test(content)).toBe(true)

      // Verify dangerous tags are NOT present
      const dangerousTags = ['<script', '<iframe', '<object', '<embed', '<form', '<input']
      dangerousTags.forEach((tag) => {
        expect(content.toLowerCase()).not.toContain(tag.toLowerCase())
      })

      // Verify event handlers are NOT present
      const eventHandlers = ['onclick', 'onerror', 'onload', 'onmouseover', 'onfocus']
      eventHandlers.forEach((handler) => {
        expect(content.toLowerCase()).not.toContain(handler.toLowerCase())
      })
    })
  })

  describe('Response Time Performance', () => {
    it('GET page by slug responds within 200ms', async () => {
      const startTime = Date.now()
      const response = await fetch(`${API_BASE_URL}/api/cms/pages/resources`)
      const responseTime = Date.now() - startTime

      expect(response.status).toBe(200)
      expect(responseTime).toBeLessThan(200) // Target from functional spec
    })
  })
})
