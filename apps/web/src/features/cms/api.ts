// CMS API Service
// Handles all CMS-related API calls including CRUD operations.
// All state-changing endpoints (create, update, delete) require Administrator role.
// CSRF tokens are automatically included by apiClient for POST/PUT/DELETE requests.

import type { ContentPageDto, UpdateContentPageRequest, ContentRevisionDto, CmsPageSummaryDto } from './types'
import { apiClient } from '../../lib/api/client'

const API_BASE_URL = '/api/cms'

/**
 * Fetch a CMS page by slug
 * Public endpoint - no authentication required
 */
export const getCmsPageBySlug = async (slug: string): Promise<ContentPageDto> => {
  const response = await apiClient.get(`${API_BASE_URL}/pages/${slug}`)
  return response.data
}

/**
 * Create a new CMS page
 * Requires Administrator role
 * Returns 409 if slug already exists (slugs are globally unique, even across deleted pages)
 */
export const createCmsPage = async (data: {
  title: string
  slug: string
  content: string
}): Promise<ContentPageDto> => {
  const response = await apiClient.post(`${API_BASE_URL}/pages`, data)
  return response.data
}

/**
 * Update a CMS page
 * Requires Administrator role
 * Note: apiClient automatically includes CSRF token
 */
export const updateCmsPage = async (
  id: number,
  data: UpdateContentPageRequest
): Promise<ContentPageDto> => {
  const response = await apiClient.put(`${API_BASE_URL}/pages/${id}`, data)
  return response.data
}

/**
 * Soft-delete a CMS page
 * Requires Administrator role
 * The page remains in the database but is excluded from all queries.
 * Slug remains reserved (cannot be reused).
 */
export const deleteCmsPage = async (id: number): Promise<void> => {
  await apiClient.delete(`${API_BASE_URL}/pages/${id}`)
}

/**
 * Get revision history for a page
 * Requires Administrator role
 */
export const getCmsRevisions = async (pageId: number): Promise<ContentRevisionDto[]> => {
  const response = await apiClient.get(`${API_BASE_URL}/pages/${pageId}/revisions`)
  return response.data
}

/**
 * Get list of all CMS pages (excludes soft-deleted pages)
 * Requires Administrator role
 */
export const getAllCmsPages = async (): Promise<CmsPageSummaryDto[]> => {
  const response = await apiClient.get(`${API_BASE_URL}/pages`)
  return response.data
}
