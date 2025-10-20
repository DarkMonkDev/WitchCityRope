import React, { useState } from 'react'
import { Title, Card, Select, Group, Text } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { WCRButton } from '../ui'
import { useUpdateMemberRole } from '../../lib/api/hooks/useMemberDetails'

interface RoleAssignmentSectionProps {
  memberId: string
  currentRole: string
}

const AVAILABLE_ROLES = [
  { value: 'Guest', label: 'Guest' },
  { value: 'Member', label: 'Member' },
  { value: 'VettedMember', label: 'Vetted Member' },
  { value: 'Teacher', label: 'Teacher' },
  { value: 'SafetyTeam', label: 'Safety Team' },
  { value: 'Admin', label: 'Admin' },
]

export const RoleAssignmentSection: React.FC<RoleAssignmentSectionProps> = ({
  memberId,
  currentRole,
}) => {
  const [selectedRole, setSelectedRole] = useState(currentRole)
  const updateRoleMutation = useUpdateMemberRole()

  const hasChanges = selectedRole !== currentRole

  const handleSave = async () => {
    if (!hasChanges) return

    try {
      await updateRoleMutation.mutateAsync({
        userId: memberId,
        request: { role: selectedRole },
      })

      notifications.show({
        title: 'Success',
        message: 'Member role updated successfully',
        color: 'green',
      })
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error.message || 'Failed to update role',
        color: 'red',
      })
      // Reset to current role on error
      setSelectedRole(currentRole)
    }
  }

  return (
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
        Role Assignment
      </Title>
      <Card withBorder p="md" radius="md">
        <Text size="sm" c="dimmed" mb="md">
          Assign a role to this member. Roles determine access permissions and capabilities within
          the application.
        </Text>

        <Group grow>
          <Select
            label="Member Role"
            placeholder="Select role..."
            data={AVAILABLE_ROLES}
            value={selectedRole}
            onChange={(value) => setSelectedRole(value || currentRole)}
          />
          <div style={{ display: 'flex', alignItems: 'flex-end' }}>
            <WCRButton
              onClick={handleSave}
              variant="primary"
              fullWidth
              disabled={!hasChanges}
              loading={updateRoleMutation.isPending}
            >
              Save Role
            </WCRButton>
          </div>
        </Group>
      </Card>
    </div>
  )
}
