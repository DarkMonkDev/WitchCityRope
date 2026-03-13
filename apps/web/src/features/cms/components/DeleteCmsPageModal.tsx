// DeleteCmsPageModal component
// Confirmation modal for soft-deleting a CMS page from the admin CMS list.
// Follows the same styling pattern as other admin modals (DenyApplicationModal, OnHoldModal).
// Soft delete means the page stays in the database but is excluded from all queries.
// There is currently no UI to view or restore soft-deleted pages.

import React, { useState } from 'react'
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { deleteCmsPage } from '../api'

interface DeleteCmsPageModalProps {
  opened: boolean
  onClose: () => void
  /** The ID of the page to delete */
  pageId: number | null
  /** The title of the page (displayed in the confirmation message) */
  pageTitle: string
  onSuccess?: () => void
}

export const DeleteCmsPageModal: React.FC<DeleteCmsPageModalProps> = ({
  opened,
  onClose,
  pageId,
  pageTitle,
  onSuccess,
}) => {
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async () => {
    if (pageId === null) return

    setIsSubmitting(true)
    try {
      await deleteCmsPage(pageId)

      notifications.show({
        title: 'Page Deleted',
        message: `"${pageTitle}" has been deleted`,
        color: 'green',
      })

      onClose()
      onSuccess?.()
    } catch (error: any) {
      const detail = error?.response?.data?.detail || error?.detail || error?.message || 'Failed to delete page'

      notifications.show({
        title: 'Error',
        message: detail,
        color: 'red',
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    if (!isSubmitting) {
      onClose()
    }
  }

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Delete Page
        </Title>
      }
      centered
      size="md"
      data-testid="delete-cms-page-modal"
    >
      <Stack gap="md">
        <Text size="sm">
          Are you sure you want to delete <strong>"{pageTitle}"</strong>?
        </Text>

        <Text size="sm" c="dimmed">
          This page will no longer be visible to visitors or in the admin list.
          The page data will be preserved in the database but cannot be restored
          from the UI.
        </Text>

        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
            data-testid="delete-page-cancel-button"
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
            color="red"
            onClick={handleSubmit}
            loading={isSubmitting}
            data-testid="delete-page-submit-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
            }}
          >
            Delete Page
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
