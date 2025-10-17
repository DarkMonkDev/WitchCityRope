// CMS Types
// NOTE: These are temporary types until backend developer generates NSwag types
// Once @witchcityrope/shared-types is updated, replace these with generated types

export interface ContentPageDto {
  id: number
  slug: string
  title: string
  content: string
  updatedAt: string
  lastModifiedBy: string
}

export interface UpdateContentPageRequest {
  title: string
  content: string
  changeDescription?: string
}

export interface ContentRevisionDto {
  id: number
  contentPageId: number
  createdAt: string
  createdBy: string
  changeDescription: string | null
  contentPreview: string
}

export interface CmsPageSummaryDto {
  id: number
  slug: string
  title: string
  revisionCount: number
  updatedAt: string
  lastModifiedBy: string
}
