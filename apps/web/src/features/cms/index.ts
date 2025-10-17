// CMS Feature Exports
// Centralized exports for CMS components, hooks, and types

// Components
export { CmsPage } from './components/CmsPage'
export { CmsEditButton } from './components/CmsEditButton'
export { CmsCancelModal } from './components/CmsCancelModal'
export { CmsRevisionCard } from './components/CmsRevisionCard'

// Pages
export { ResourcesPage } from './pages/ResourcesPage'
export { ContactUsPage } from './pages/ContactUsPage'
export { PrivateLessonsPage } from './pages/PrivateLessonsPage'
export { CmsRevisionListPage } from './pages/CmsRevisionListPage'
export { CmsRevisionDetailPage } from './pages/CmsRevisionDetailPage'

// Hooks
export { useCmsPage } from './hooks/useCmsPage'
export { useCmsRevisions } from './hooks/useCmsRevisions'
export { useCmsPageList } from './hooks/useCmsPageList'

// Types
export type {
  ContentPageDto,
  UpdateContentPageRequest,
  ContentRevisionDto,
  CmsPageSummaryDto,
} from './types'
