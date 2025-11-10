// EventWaiverPage component
// CMS page for Event Waiver (/event-waiver)

import React from 'react'
import { CmsPage } from '../components/CmsPage'

export const EventWaiverPage: React.FC = () => {
  return (
    <CmsPage
      slug="event-waiver"
      defaultTitle="Event Waiver"
      defaultContent="<h1>Witch City Rope Event Waiver</h1><p>Event waiver and liability release...</p>"
    />
  )
}
