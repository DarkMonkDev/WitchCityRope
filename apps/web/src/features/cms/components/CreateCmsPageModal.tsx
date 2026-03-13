// CreateCmsPageModal component
// Modal for creating a new CMS content page from the admin CMS list.
// Follows the same styling pattern as other admin modals (DenyApplicationModal, OnHoldModal).
// Collects title, slug, and initial content. Slug must be globally unique.

import React, { useState } from 'react'
import {
  Modal,
  Stack,
  Text,
  TextInput,
  Textarea,
  Button,
  Group,
  Title,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { createCmsPage } from '../api'

interface CreateCmsPageModalProps {
  opened: boolean
  onClose: () => void
  onSuccess?: () => void
}

export const CreateCmsPageModal: React.FC<CreateCmsPageModalProps> = ({
  opened,
  onClose,
  onSuccess,
}) => {
  const [title, setTitle] = useState('')
  const [slug, setSlug] = useState('')
  const [content, setContent] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const resetForm = () => {
    setTitle('')
    setSlug('')
    setContent('')
  }

  const handleSubmit = async () => {
    // Client-side validation
    if (!title.trim() || title.trim().length < 3) {
      notifications.show({
        title: 'Validation Error',
        message: 'Title must be at least 3 characters',
        color: 'yellow',
      })
      return
    }

    if (!slug.trim()) {
      notifications.show({
        title: 'Validation Error',
        message: 'Slug is required',
        color: 'yellow',
      })
      return
    }

    // Validate slug format matches the DB constraint: lowercase alphanumeric with hyphens
    const slugPattern = /^[a-z0-9]+(-[a-z0-9]+)*$/
    if (!slugPattern.test(slug.trim())) {
      notifications.show({
        title: 'Validation Error',
        message: 'Slug must contain only lowercase letters, numbers, and hyphens (e.g., "about-us")',
        color: 'yellow',
      })
      return
    }

    if (!content.trim()) {
      notifications.show({
        title: 'Validation Error',
        message: 'Content is required',
        color: 'yellow',
      })
      return
    }

    setIsSubmitting(true)
    try {
      await createCmsPage({
        title: title.trim(),
        slug: slug.trim(),
        content: content.trim(),
      })

      notifications.show({
        title: 'Page Created',
        message: `"${title.trim()}" has been created successfully`,
        color: 'green',
      })

      resetForm()
      onClose()
      onSuccess?.()
    } catch (error: any) {
      // Handle 409 Conflict (duplicate slug) specifically
      const statusCode = error?.response?.status
      const detail = error?.response?.data?.detail || error?.detail || error?.message || 'Failed to create page'

      notifications.show({
        title: statusCode === 409 ? 'Slug Already Exists' : 'Error',
        message: detail,
        color: 'red',
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    if (!isSubmitting) {
      resetForm()
      onClose()
    }
  }

  // Determine if the form has enough data to submit
  const canSubmit = title.trim().length >= 3 && slug.trim().length > 0 && content.trim().length > 0

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Create New Page
        </Title>
      }
      centered
      size="lg"
      data-testid="create-cms-page-modal"
    >
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          Create a new CMS-managed page. The page will be published immediately
          but won't be linked anywhere until you add it to the navigation.
        </Text>

        <TextInput
          label="Page Title"
          placeholder="Community Resources"
          value={title}
          onChange={(e) => setTitle(e.currentTarget.value)}
          required
          maxLength={200}
          data-testid="create-page-title-input"
          styles={{
            input: {
              borderRadius: '8px',
              border: '1px solid #E0E0E0',
            },
          }}
        />

        <TextInput
          label="URL Slug"
          placeholder="about-us"
          description="Lowercase letters, numbers, and hyphens only. This becomes the page URL (e.g., /about-us)."
          value={slug}
          onChange={(e) => setSlug(e.currentTarget.value.toLowerCase())}
          required
          maxLength={100}
          data-testid="create-page-slug-input"
          styles={{
            input: {
              borderRadius: '8px',
              border: '1px solid #E0E0E0',
            },
          }}
        />

        <Textarea
          label="Content"
          placeholder="Enter the initial page content (HTML supported)..."
          value={content}
          onChange={(e) => setContent(e.currentTarget.value)}
          minRows={6}
          required
          data-testid="create-page-content-input"
          styles={{
            input: {
              borderRadius: '8px',
              border: '1px solid #E0E0E0',
            },
          }}
        />

        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
            data-testid="create-page-cancel-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
            }}
          >
            Cancel
          </Button>
          <Button
            color="green"
            onClick={handleSubmit}
            loading={isSubmitting}
            disabled={!canSubmit}
            data-testid="create-page-submit-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
            }}
          >
            Create Page
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
