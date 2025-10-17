// PrivateLessonsPage component
// CMS page for Private Lessons (/private-lessons)

import React from 'react'
import { CmsPage } from '../components/CmsPage'

export const PrivateLessonsPage: React.FC = () => {
  return (
    <CmsPage
      slug="private-lessons"
      defaultTitle="Private Lessons"
      defaultContent="<h1>Private Lessons</h1><p>Learn about our private instruction offerings...</p>"
    />
  )
}
