// CmsDynamicPage component
// Dynamic CMS page that uses URL slug parameter to load content.
// Shows NotFoundPage when the CMS API returns 404 for a non-existent slug,
// following the Dark Monk pattern where content-not-found → branded 404 page.

import React from 'react'
import { useParams } from 'react-router-dom'
import { Container, Alert } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { CmsPage } from '../components/CmsPage'
import { useCmsPage } from '../hooks/useCmsPage'
import { NotFoundPage } from '../../../pages/NotFoundPage'
import { isAxiosError } from 'axios'

/**
 * Dynamic CMS page component that loads content based on URL slug parameter.
 *
 * Route: /:slug (catch-all for single-segment paths)
 * Examples:
 *   /resources → loads CMS page with slug "resources"
 *   /about-us → loads CMS page with slug "about-us"
 *   /terms-of-service → loads CMS page with slug "terms-of-service"
 *
 * This component replaces 11+ individual page components that just wrapped <CmsPage />.
 * New CMS pages can be added via database seed without code deployment.
 *
 * 404 Handling:
 * - When the CMS API returns 404 (slug doesn't exist), renders NotFoundPage
 *   instead of a generic error message. This is the primary 404 path for
 *   single-segment unknown URLs (e.g., /asdfgh, /nonexistent-page).
 * - Non-404 errors (500, network failure) still show a generic error alert
 *   since those indicate a server problem, not a missing page.
 */
export const CmsDynamicPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>()

  // Validate slug parameter exists (shouldn't happen with React Router, but defensive check)
  if (!slug) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle />} color="red" title="Invalid URL">
          No page slug provided in URL.
        </Alert>
      </Container>
    )
  }

  // Use the CMS hook to check for 404 before rendering CmsPage.
  // This allows us to distinguish "page doesn't exist" (404) from
  // "server error" (500) and show the appropriate UI for each.
  return <CmsDynamicPageContent slug={slug} />
}

/**
 * Inner component that uses the CMS hook to detect 404 errors.
 * Separated from CmsDynamicPage because hooks can't be called after
 * early returns (React rules of hooks).
 */
const CmsDynamicPageContent: React.FC<{ slug: string }> = ({ slug }) => {
  const { error, isLoading } = useCmsPage(slug)

  // While loading, let CmsPage handle the loading state UI
  if (isLoading) {
    return <CmsPage slug={slug} defaultTitle="Page" />
  }

  // If the CMS API returned 404, the page/slug doesn't exist in the CMS database.
  // Show the branded NotFoundPage instead of a generic error message.
  // This is the main 404 path for URLs like /nonexistent-page that match the
  // :slug route but don't correspond to any published CMS content.
  if (error && isAxiosError(error) && error.response?.status === 404) {
    return <NotFoundPage />
  }

  // For all other cases (success, non-404 errors), delegate to CmsPage
  // which handles its own error display and admin editing UI
  return <CmsPage slug={slug} defaultTitle="Page" />
}
