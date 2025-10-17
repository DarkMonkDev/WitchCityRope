// useCmsPageList hook
// Fetch list of all CMS pages with revision counts

import { useQuery } from '@tanstack/react-query'
import { getAllCmsPages } from '../api'
import type { CmsPageSummaryDto } from '../types'

export const useCmsPageList = () => {
  return useQuery<CmsPageSummaryDto[]>({
    queryKey: ['cms-pages'],
    queryFn: getAllCmsPages,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  })
}
