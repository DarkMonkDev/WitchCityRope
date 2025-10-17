// ContactUsPage component
// CMS page for Contact Us (/contact-us)

import React from 'react'
import { CmsPage } from '../components/CmsPage'

export const ContactUsPage: React.FC = () => {
  return (
    <CmsPage
      slug="contact-us"
      defaultTitle="Contact Us"
      defaultContent="<h1>Contact Us</h1><p>Get in touch with the WitchCityRope team...</p>"
    />
  )
}
