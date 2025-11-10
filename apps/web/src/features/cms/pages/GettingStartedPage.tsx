// GettingStartedPage component
// CMS page for Getting Started (/cms/getting-started)

import React from 'react'
import { CmsPage } from '../components/CmsPage'

export const GettingStartedPage: React.FC = () => {
  return (
    <CmsPage
      slug="getting-started"
      defaultTitle="Getting Started with Shibari"
      defaultContent="<h1>Getting Started with Shibari</h1><p>Welcome to your rope journey...</p>"
    />
  )
}
