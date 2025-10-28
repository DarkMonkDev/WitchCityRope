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
          <TextInput
            label="Page Title"
            placeholder="Enter page title"
            value={editableTitle}
            onChange={handleTitleChange}
            required
            mb="md"
            styles={{
              input: {
                fontFamily: 'Montserrat, sans-serif',
                fontSize: '24px',
                fontWeight: 600,
              },
            }}
          />

          <MantineTiptapEditor
            value={editableContent}
            onChange={handleContentChange}
            placeholder="Enter page content..."
            minRows={15}
          />

          <Group justify="flex-end" mt="lg">
            <Button
              variant="outline"
              onClick={handleCancel}
              disabled={isSaving}
              size="md"
            >
              Cancel
            </Button>
            <Button
              onClick={handleSave}
              loading={isSaving}
              disabled={!isDirty}
              size="md"
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
          <h1
            style={{
              fontFamily: 'Montserrat, sans-serif',
              fontSize: '32px',
              fontWeight: 700,
              marginBottom: '24px',
              color: '#1a1a1a',
            }}
          >
            {pageContent.title}
          </h1>

          {/* Display page content */}
          <div dangerouslySetInnerHTML={{ __html: pageContent.content }} />
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
