// CMS API Service
// Handles all CMS-related API calls

import type { ContentPageDto, UpdateContentPageRequest, ContentRevisionDto, CmsPageSummaryDto } from './types'

const API_BASE_URL = '/api/cms'

/**
 * Fetch a CMS page by slug
 * Public endpoint - no authentication required
 */
export const getCmsPageBySlug = async (slug: string): Promise<ContentPageDto> => {
  const response = await fetch(`${API_BASE_URL}/pages/${slug}`, {
    credentials: 'include', // Include httpOnly cookies
  })

  if (!response.ok) {
    if (response.status === 404) {
      throw new Error(`Page with slug "${slug}" not found`)
    }
    throw new Error(`Failed to fetch page: ${response.statusText}`)
  }

  return response.json()
}

/**
 * Update a CMS page
 * Requires Administrator role
 */
export const updateCmsPage = async (
  id: number,
  data: UpdateContentPageRequest
): Promise<ContentPageDto> => {
  const response = await fetch(`${API_BASE_URL}/pages/${id}`, {
    method: 'PUT',
    credentials: 'include', // Include httpOnly cookies for authentication
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Authentication required. Please log in.')
    }
    if (response.status === 403) {
      throw new Error('Administrator role required to edit content.')
    }
    if (response.status === 400) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Validation failed')
    }
    throw new Error(`Failed to update page: ${response.statusText}`)
  }

  return response.json()
}

/**
 * Get revision history for a page
 * Requires Administrator role
 */
export const getCmsRevisions = async (pageId: number): Promise<ContentRevisionDto[]> => {
  const response = await fetch(`${API_BASE_URL}/pages/${pageId}/revisions`, {
    credentials: 'include',
  })

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Authentication required')
    }
    if (response.status === 403) {
      throw new Error('Administrator role required')
    }
    throw new Error(`Failed to fetch revisions: ${response.statusText}`)
  }

  return response.json()
}

/**
 * Get list of all CMS pages
 * Requires Administrator role
 */
export const getAllCmsPages = async (): Promise<CmsPageSummaryDto[]> => {
  const response = await fetch(`${API_BASE_URL}/pages`, {
    credentials: 'include',
  })

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Authentication required')
    }
    if (response.status === 403) {
      throw new Error('Administrator role required')
    }
    throw new Error(`Failed to fetch pages: ${response.statusText}`)
  }

  return response.json()
}
