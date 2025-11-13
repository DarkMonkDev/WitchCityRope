// CmsDynamicPage component
// Dynamic CMS page that uses URL slug parameter to load content
// Replaces individual CMS page components (ResourcesPage, AboutUsPage, etc.)

import React from 'react'
import { useParams } from 'react-router-dom'
import { Container, Alert } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { CmsPage } from '../components/CmsPage'

/**
 * Dynamic CMS page component that loads content based on URL slug parameter
 *
 * Route: /:slug
 * Examples:
 *   /resources → loads CMS page with slug "resources"
 *   /about-us → loads CMS page with slug "about-us"
 *   /terms-of-service → loads CMS page with slug "terms-of-service"
 *
 * This component replaces 11+ individual page components that just wrapped <CmsPage />
 * New CMS pages can be added via database seed without code deployment
 */
export const CmsDynamicPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>()

  // Validate slug parameter exists
  if (!slug) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle />} color="red" title="Invalid URL">
          No page slug provided in URL.
        </Alert>
      </Container>
    )
  }

  // Render CmsPage with slug from URL
  return <CmsPage slug={slug} defaultTitle="Page" />
}
