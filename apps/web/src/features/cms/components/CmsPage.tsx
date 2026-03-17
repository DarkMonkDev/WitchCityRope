// CmsPage component
// Main editing component for CMS pages

import React, { useState, useEffect } from 'react'
import { Box, Container, TextInput, Group, Button, LoadingOverlay, Alert } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { MantineTiptapEditor } from '../../../components/forms/MantineTiptapEditor'
import { CmsEditButton } from './CmsEditButton'
import { CmsCancelModal } from './CmsCancelModal'
import { useCmsPage } from '../hooks/useCmsPage'
import { useUser } from '../../../stores/authStore'
import { useViewportSize } from '@mantine/hooks'
import { sanitizeHtml } from '../../../lib/utils/sanitizeHtml'
import { hasRole } from '../../../utils/roleUtils'

interface CmsPageProps {
  slug: string
  defaultTitle?: string
  defaultContent?: string
}

export const CmsPage: React.FC<CmsPageProps> = ({ slug, defaultTitle, defaultContent }) => {
  const user = useUser()
  const isAdmin = hasRole(user, 'Administrator')
  const { width: viewportWidth } = useViewportSize()

  const { content, isLoading, save, isSaving, error } = useCmsPage(slug)

  const [isEditing, setIsEditing] = useState(false)
  const [editableContent, setEditableContent] = useState('')
  const [editableTitle, setEditableTitle] = useState('')
  const [editableSlug, setEditableSlug] = useState('')
  const [isDirty, setIsDirty] = useState(false)
  const [showCancelModal, setShowCancelModal] = useState(false)

  // Browser beforeunload warning for unsaved changes
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (isDirty && isEditing) {
        e.preventDefault()
        e.returnValue = '' // Chrome requires returnValue to be set
      }
    }

    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => window.removeEventListener('beforeunload', handleBeforeUnload)
  }, [isDirty, isEditing])

  // Keyboard event handler for Escape key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isEditing) {
        e.preventDefault()
        // Handle cancel logic inline to avoid dependency issues
        if (isDirty) {
          setShowCancelModal(true)
        } else {
          setIsEditing(false)
        }
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isEditing, isDirty])

  const handleEdit = () => {
    if (content) {
      setEditableTitle(content.title || '')
      setEditableSlug(content.slug || '')
      setEditableContent(content.content || '')
    }
    setIsEditing(true)
    setIsDirty(false)
  }

  const handleSave = async () => {
    try {
      await save({
        title: editableTitle,
        content: editableContent,
        slug: editableSlug || undefined,
      })
      setIsEditing(false)
      setIsDirty(false)
    } catch (err) {
      // Error is handled by the hook's onError
      console.error('Save error:', err)
    }
  }

  const handleCancel = () => {
    if (isDirty) {
      setShowCancelModal(true)
    } else {
      setIsEditing(false)
    }
  }

  const handleConfirmDiscard = () => {
    setShowCancelModal(false)
    setIsEditing(false)
    setIsDirty(false)
  }

  const handleContentChange = (html: string) => {
    setEditableContent(html)
    setIsDirty(true)
  }

  const handleTitleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setEditableTitle(e.target.value)
    setIsDirty(true)
  }

  const handleSlugChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    // Force lowercase as user types to match slug format requirements
    setEditableSlug(e.target.value.toLowerCase())
    setIsDirty(true)
  }

  if (isLoading) {
    return (
      <Container size="lg">
        <LoadingOverlay visible />
      </Container>
    )
  }

  if (error && !content) {
    return (
      <Container size="lg">
        <Alert icon={<IconAlertCircle />} color="red" title="Error">
          Failed to load page content. Please try again later.
        </Alert>
      </Container>
    )
  }

  const pageContent = content || { title: defaultTitle || '', content: defaultContent || '' }

  return (
    <Container size="lg" py="xl">
      {/* Edit button (admin-only) */}
      {isAdmin && !isEditing && <CmsEditButton onClick={handleEdit} viewportWidth={viewportWidth} />}

      {/* Edit mode */}
      {isEditing && (
        <Box>
          {/* Title and Slug row - each takes half width */}
          <Group align="flex-end" mb="md" wrap="nowrap" gap="md">
            {/* Title input - left half */}
            <Box style={{ flex: '1 1 50%', minWidth: 0 }}>
              <TextInput
                label="Page Title"
                placeholder="Enter page title"
                value={editableTitle}
                onChange={handleTitleChange}
                required
                styles={{
                  input: {
                    fontFamily: 'Montserrat, sans-serif',
                    fontSize: '24px',
                    fontWeight: 600,
                  },
                }}
              />
            </Box>

            {/* Slug input - right half */}
            <Box style={{ flex: '1 1 50%', minWidth: 0 }}>
              <TextInput
                label="URL Slug"
                placeholder="about-us"
                description="Lowercase letters, numbers, and hyphens only"
                value={editableSlug}
                onChange={handleSlugChange}
                required
                styles={{
                  input: {
                    fontFamily: 'monospace',
                    fontSize: '16px',
                  },
                }}
              />
            </Box>
          </Group>

          {/* Last modified and action buttons row */}
          <Group justify="space-between" align="center" mb="md" wrap="nowrap">
            {/* Last Modified Date - only show if available */}
            {content?.updatedAt && (
              <Box
                style={{
                  fontSize: '14px',
                  color: '#5c5f66', // Darker gray for WCAG AA compliance (4.5:1 contrast ratio)
                  whiteSpace: 'nowrap',
                }}
              >
                Last Modified: {new Date(content.updatedAt).toLocaleString()}
              </Box>
            )}

            {/* Save and Cancel buttons */}
            <Group gap="sm" wrap="nowrap" style={{ flexShrink: 0, marginLeft: 'auto' }}>
              <Button
                variant="outline"
                onClick={handleCancel}
                disabled={isSaving}
                size="md"
                color="gray"
                styles={{
                  root: {
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                    // WCAG AA compliance: Ensure sufficient color contrast (4.5:1 ratio)
                    borderColor: 'var(--mantine-color-gray-7)',
                    color: 'var(--mantine-color-gray-9)',
                  },
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={handleSave}
                loading={isSaving}
                disabled={!isDirty}
                size="md"
                color="blue"
                styles={{
                  root: {
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                  },
                }}
              >
                Save
              </Button>
            </Group>
          </Group>

          <MantineTiptapEditor
            value={editableContent}
            onChange={handleContentChange}
            placeholder="Enter page content..."
            minRows={15}
          />

          {/* Bottom Save and Cancel buttons */}
          <Group justify="flex-end" gap="sm" mt="md">
            <Button
              variant="outline"
              onClick={handleCancel}
              disabled={isSaving}
              size="md"
              color="gray"
              styles={{
                root: {
                  fontWeight: 600,
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                  // WCAG AA compliance: Ensure sufficient color contrast (4.5:1 ratio)
                  borderColor: 'var(--mantine-color-gray-7)',
                  color: 'var(--mantine-color-gray-9)',
                },
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSave}
              loading={isSaving}
              disabled={!isDirty}
              size="md"
              color="blue"
              styles={{
                root: {
                  fontWeight: 600,
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                },
              }}
            >
              Save
            </Button>
          </Group>
        </Box>
      )}

      {/* View mode */}
      {!isEditing && (
        <Box>
          {/* Display page content with global HTML content styling */}
          <div className="html-content" dangerouslySetInnerHTML={{ __html: sanitizeHtml(pageContent.content || '') }} />
        </Box>
      )}

      {/* Cancel confirmation modal */}
      <CmsCancelModal
        opened={showCancelModal}
        onClose={() => setShowCancelModal(false)}
        onConfirm={handleConfirmDiscard}
      />
    </Container>
  )
}
