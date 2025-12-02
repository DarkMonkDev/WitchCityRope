import React, { useState } from 'react'
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Select,
  Textarea,
  Checkbox,
  Alert,
} from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'

// VettingStatus type from @witchcityrope/shared-types
type VettingStatus = 'UnderReview' | 'InterviewApproved' | 'FinalReview' | 'Approved' | 'Denied' | 'OnHold' | 'Withdrawn'

interface ChangeVettingStatusModalProps {
  opened: boolean
  onClose: () => void
  onConfirm: (newStatus: string, reason: string) => Promise<void>
  currentStatus: string
  memberName: string
  isLoading?: boolean
}

// VettingStatus display labels
const VETTING_STATUS_OPTIONS: Array<{ value: VettingStatus; label: string }> = [
  { value: 'UnderReview', label: 'Under Review' },
  { value: 'InterviewApproved', label: 'Interview Approved' },
  { value: 'FinalReview', label: 'Final Review' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Denied', label: 'Denied' },
  { value: 'OnHold', label: 'On Hold' },
  { value: 'Withdrawn', label: 'Withdrawn' },
]

export const ChangeVettingStatusModal: React.FC<ChangeVettingStatusModalProps> = ({
  opened,
  onClose,
  onConfirm,
  currentStatus,
  memberName,
  isLoading = false,
}) => {
  const [selectedStatus, setSelectedStatus] = useState<string>('')
  const [reason, setReason] = useState<string>('')
  const [confirmed, setConfirmed] = useState<boolean>(false)

  // Reset form when modal closes
  const handleClose = () => {
    if (!isLoading) {
      setSelectedStatus('')
      setReason('')
      setConfirmed(false)
      onClose()
    }
  }

  // Submit handler
  const handleSubmit = async () => {
    if (!selectedStatus || !reason || !confirmed) return

    try {
      await onConfirm(selectedStatus, reason)
      handleClose() // Close and reset on success
    } catch (error) {
      // Error handling is done by parent component via notifications
      console.error('Failed to change vetting status:', error)
    }
  }

  // Check if form is valid
  const isFormValid = selectedStatus && reason.length >= 10 && confirmed

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Text fw={600} size="lg" c="burgundy">
          Change Vetting Status
        </Text>
      }
      centered
      size="md"
      data-testid="change-vetting-status-modal"
    >
      <Stack gap="md">
        {/* Member Info */}
        <Text size="sm">
          Member:{' '}
          <Text component="span" fw={600}>
            {memberName}
          </Text>
        </Text>

        {/* Current Status */}
        <Alert icon={<IconAlertCircle size={20} />} color="blue" variant="light">
          <Text size="sm" fw={600}>
            Current Status: {currentStatus}
          </Text>
        </Alert>

        {/* New Status Selection */}
        <Select
          label="New Status"
          placeholder="Select new vetting status"
          data={VETTING_STATUS_OPTIONS}
          value={selectedStatus}
          onChange={(value) => setSelectedStatus(value || '')}
          required
          data-testid="status-select"
          styles={{
            root: {
              fontWeight: 600,
            },
          }}
        />

        {/* Reason Textarea */}
        <Textarea
          label="Reason for Change"
          placeholder="Enter reason for status change (minimum 10 characters)"
          value={reason}
          onChange={(event) => setReason(event.currentTarget.value)}
          minRows={4}
          required
          error={reason.length > 0 && reason.length < 10 ? 'Reason must be at least 10 characters' : null}
          data-testid="reason-textarea"
          styles={{
            label: {
              fontWeight: 600,
            },
          }}
        />

        {/* Confirmation Checkbox */}
        <Checkbox
          label="I confirm I want to change this member's vetting status"
          checked={confirmed}
          onChange={(event) => setConfirmed(event.currentTarget.checked)}
          data-testid="confirm-checkbox"
          styles={{
            label: {
              fontSize: '14px',
            },
          }}
        />

        {/* Action Buttons */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isLoading}
            data-testid="cancel-button"
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
            Cancel
          </Button>
          <Button
            color="grape"
            onClick={handleSubmit}
            loading={isLoading}
            disabled={!isFormValid}
            data-testid="submit-button"
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
            Change Status
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
