// ResourcesPage component
// CMS page for Resources (/resources)

import React from 'react'
import { CmsPage } from '../components/CmsPage'

export const ResourcesPage: React.FC = () => {
  return (
    <CmsPage
      slug="resources"
      defaultTitle="Community Resources"
      defaultContent="<h1>Community Resources</h1><p>Welcome to our comprehensive guide to rope bondage safety and resources...</p>"
    />
  )
}
