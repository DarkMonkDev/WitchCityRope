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
import type { components } from '@witchcityrope/shared-types'

interface CmsPageProps {
  slug: string
  defaultTitle?: string
  defaultContent?: string
}

export const CmsPage: React.FC<CmsPageProps> = ({ slug, defaultTitle, defaultContent }) => {
  const user = useUser()
  // Type-safe role check using auto-generated UserRole type
  type UserRole = components['schemas']['UserRole']
  const isAdmin = user?.role === ('Administrator' as UserRole)
  const { width: viewportWidth } = useViewportSize()

  const { content, isLoading, save, isSaving, error } = useCmsPage(slug)

  const [isEditing, setIsEditing] = useState(false)
  const [editableContent, setEditableContent] = useState('')
  const [editableTitle, setEditableTitle] = useState('')
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
      setEditableTitle(content.title)
      setEditableContent(content.content)
    }
    setIsEditing(true)
    setIsDirty(false)
  }

  const handleSave = async () => {
    try {
      await save({
        title: editableTitle,
        content: editableContent,
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
          {/* Title row with inline label, input, last modified, and buttons */}
          <Group justify="space-between" align="flex-end" mb="md" wrap="nowrap">
            {/* Title input section - takes majority of space */}
            <Box style={{ flex: '1 1 auto', minWidth: 0 }}>
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

            {/* Last Modified Date - only show if available */}
            {content?.updatedAt && (
              <Box
                style={{
                  fontSize: '14px',
                  color: '#5c5f66', // Darker gray for WCAG AA compliance (4.5:1 contrast ratio)
                  whiteSpace: 'nowrap',
                  paddingBottom: '8px',
                  marginLeft: '16px',
                  marginRight: '16px',
                }}
              >
                Last Modified: {new Date(content.updatedAt).toLocaleString()}
              </Box>
            )}

            {/* Save and Cancel buttons */}
            <Group gap="sm" wrap="nowrap" style={{ flexShrink: 0 }}>
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
          {/* Display page title as H1 */}
          <h1 className="page-title">
            {pageContent.title}
          </h1>

          {/* Display page content with global HTML content styling */}
          <div className="html-content" dangerouslySetInnerHTML={{ __html: pageContent.content }} />
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
