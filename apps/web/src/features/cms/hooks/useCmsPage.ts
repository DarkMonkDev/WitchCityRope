// useCmsPage hook
// Fetch and update CMS page content with optimistic updates

import React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notifications } from '@mantine/notifications'
import { IconCheck, IconX } from '@tabler/icons-react'
import { getCmsPageBySlug, updateCmsPage } from '../api'
import type { ContentPageDto, UpdateContentPageRequest } from '../types'

export const useCmsPage = (slug: string) => {
  const queryClient = useQueryClient()

  // Fetch page content
  const query = useQuery<ContentPageDto>({
    queryKey: ['cms-page', slug],
    queryFn: () => getCmsPageBySlug(slug),
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
    retryDelay: 1000,
  })

  // Update mutation with optimistic updates
  const mutation = useMutation({
    mutationFn: (data: UpdateContentPageRequest) => {
      if (!query.data?.id) {
        throw new Error('Page ID not available')
      }
      return updateCmsPage(query.data.id, data)
    },

    // Optimistic update (instant UI feedback)
    onMutate: async (newData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['cms-page', slug] })

      // Snapshot previous value for rollback
      const previousData = queryClient.getQueryData(['cms-page', slug]) as ContentPageDto | undefined

      // Optimistically update cache
      queryClient.setQueryData(['cms-page', slug], (old: ContentPageDto | undefined) => {
        if (!old) return old
        return {
          ...old,
          content: newData.content,
          title: newData.title,
          updatedAt: new Date().toISOString(),
        }
      })

      // Show loading notification
      notifications.show({
        id: 'cms-save',
        loading: true,
        title: 'Saving',
        message: 'Saving changes...',
        autoClose: false,
        withCloseButton: false,
      })

      return { previousData }
    },

    // Rollback on error
    onError: (err: Error, newData, context) => {
      // Restore previous data
      if (context?.previousData) {
        queryClient.setQueryData(['cms-page', slug], context.previousData)
      }

      // Show error notification
      notifications.update({
        id: 'cms-save',
        color: 'red',
        title: 'Error',
        message: err.message || 'Failed to save content. Please try again.',
        icon: React.createElement(IconX),
        autoClose: 5000,
      })

      console.error('Save failed:', err)
    },

    // Success
    onSuccess: () => {
      notifications.update({
        id: 'cms-save',
        color: 'green',
        title: 'Success',
        message: 'Content saved successfully',
        icon: React.createElement(IconCheck),
        autoClose: 3000,
      })
    },

    // Always refetch after mutation completes
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['cms-page', slug] })
    },
  })

  return {
    content: query.data,
    isLoading: query.isLoading,
    error: query.error,
    save: mutation.mutateAsync,
    isSaving: mutation.isPending,
    refetch: query.refetch,
  }
}
