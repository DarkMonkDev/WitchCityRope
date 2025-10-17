// CmsCancelModal component
// Mantine Modal for unsaved changes confirmation

import React from 'react'
import { Modal, Button, Group, Text } from '@mantine/core'

interface CmsCancelModalProps {
  opened: boolean
  onClose: () => void
  onConfirm: () => void
}

export const CmsCancelModal: React.FC<CmsCancelModalProps> = ({ opened, onClose, onConfirm }) => {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Unsaved Changes"
      centered
      size="sm"
      closeOnClickOutside={false}
    >
      <Text mb="lg">You have unsaved changes. Are you sure you want to discard them?</Text>

      <Group justify="flex-end">
        <Button
          variant="default"
          onClick={onClose}
          size="md"
        >
          Keep Editing
        </Button>
        <Button
          color="red"
          onClick={onConfirm}
          size="md"
        >
          Discard Changes
        </Button>
      </Group>
    </Modal>
  )
}
