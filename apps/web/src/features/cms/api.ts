// CMS API Service
// Handles all CMS-related API calls

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
 * Get revision history for a page
 * Requires Administrator role
 */
export const getCmsRevisions = async (pageId: number): Promise<ContentRevisionDto[]> => {
  const response = await apiClient.get(`${API_BASE_URL}/pages/${pageId}/revisions`)
  return response.data
}

/**
 * Get list of all CMS pages
 * Requires Administrator role
 */
export const getAllCmsPages = async (): Promise<CmsPageSummaryDto[]> => {
  const response = await apiClient.get(`${API_BASE_URL}/pages`)
  return response.data
}
