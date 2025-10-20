import React, { useState } from 'react'
import { Title, Card, Switch, Group, Text, Textarea, Modal, Stack } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { WCRButton } from '../ui'
import { useUpdateMemberStatus } from '../../lib/api/hooks/useMemberDetails'

interface StatusManagementSectionProps {
  memberId: string
  currentStatus: boolean
  sceneName: string
}

export const StatusManagementSection: React.FC<StatusManagementSectionProps> = ({
  memberId,
  currentStatus,
  sceneName,
}) => {
  const [desiredStatus, setDesiredStatus] = useState(currentStatus)
  const [inactiveReason, setInactiveReason] = useState('')
  const [showReasonModal, setShowReasonModal] = useState(false)
  const updateStatusMutation = useUpdateMemberStatus()

  const handleStatusToggle = (newStatus: boolean) => {
    if (!newStatus) {
      // Toggling to inactive - show reason modal
      setDesiredStatus(newStatus)
      setShowReasonModal(true)
    } else {
      // Toggling to active - update immediately
      setDesiredStatus(newStatus)
      handleSave(newStatus, undefined)
    }
  }

  const handleSave = async (status: boolean, reason?: string) => {
    try {
      await updateStatusMutation.mutateAsync({
        userId: memberId,
        request: {
          isActive: status,
          reason: reason,
        },
      })

      notifications.show({
        title: 'Success',
        message: `Member status updated to ${status ? 'Active' : 'Inactive'}`,
        color: 'green',
      })

      setShowReasonModal(false)
      setInactiveReason('')
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error.message || 'Failed to update status',
        color: 'red',
      })
      // Reset to current status on error
      setDesiredStatus(currentStatus)
    }
  }

  const handleReasonSubmit = () => {
    if (!inactiveReason || inactiveReason.trim().length === 0) {
      notifications.show({
        title: 'Validation Error',
        message: 'Please provide a reason for setting member to inactive',
        color: 'orange',
      })
      return
    }

    handleSave(false, inactiveReason.trim())
  }

  return (
    <>
      <div>
        <Title
          order={2}
          c="burgundy"
          mb="md"
          style={{
            borderBottom: '2px solid var(--mantine-color-burgundy-3)',
            paddingBottom: '8px',
          }}
        >
          Status Management
        </Title>
        <Card withBorder p="md" radius="md">
          <Group justify="space-between">
            <div>
              <Text fw={500} mb={4}>
                Member Status
              </Text>
              <Text size="sm" c="dimmed">
                Set member to Active or Inactive. Inactive members cannot access the system.
              </Text>
            </div>
            <Switch
              size="lg"
              checked={desiredStatus}
              onChange={(event) => handleStatusToggle(event.currentTarget.checked)}
              onLabel="Active"
              offLabel="Inactive"
              disabled={updateStatusMutation.isPending}
            />
          </Group>
        </Card>
      </div>

      {/* Inactive Reason Modal */}
      <Modal
        opened={showReasonModal}
        onClose={() => {
          setShowReasonModal(false)
          setDesiredStatus(currentStatus) // Reset toggle on cancel
          setInactiveReason('')
        }}
        title={<Text fw={600} size="lg">Set {sceneName} to Inactive</Text>}
        size="md"
      >
        <Stack gap="md">
          <Text size="sm" c="dimmed">
            Please provide a reason for setting this member to inactive. This will be recorded in
            their notes.
          </Text>

          <Textarea
            label="Reason for Inactivation"
            placeholder="Enter reason..."
            required
            minRows={4}
            maxRows={8}
            value={inactiveReason}
            onChange={(event) => setInactiveReason(event.currentTarget.value)}
          />

          <Group justify="flex-end" mt="md">
            <WCRButton
              variant="outline"
              onClick={() => {
                setShowReasonModal(false)
                setDesiredStatus(currentStatus)
                setInactiveReason('')
              }}
            >
              Cancel
            </WCRButton>
            <WCRButton
              variant="primary"
              onClick={handleReasonSubmit}
              loading={updateStatusMutation.isPending}
            >
              Confirm Inactive
            </WCRButton>
          </Group>
        </Stack>
      </Modal>
    </>
  )
}
