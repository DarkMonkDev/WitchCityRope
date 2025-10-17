// useCmsRevisions hook
// Fetch revision history for a CMS page

import { useQuery } from '@tanstack/react-query'
import { getCmsRevisions } from '../api'
import type { ContentRevisionDto } from '../types'

export const useCmsRevisions = (pageId: number) => {
  return useQuery<ContentRevisionDto[]>({
    queryKey: ['cms-revisions', pageId],
    queryFn: () => getCmsRevisions(pageId),
    staleTime: 2 * 60 * 1000, // 2 minutes
    retry: 2,
    enabled: !!pageId, // Only run query if pageId is provided
  })
}
