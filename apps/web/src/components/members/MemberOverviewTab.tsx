import React, { useState } from 'react'
import { Stack, Title, Card, Text, Group, Badge, Grid, Paper, Alert, MultiSelect } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useMemberDetails, useMemberNotes, useMemberVetting, useUpdateMemberRole } from '../../lib/api/hooks/useMemberDetails'
import { MemberNotesSection } from './MemberNotesSection'
import { notifications } from '@mantine/notifications'
import { useValidRoles, formatRolesForSelect } from '../../lib/api/hooks/useValidRoles'

interface MemberOverviewTabProps {
  memberId: string
}

export const MemberOverviewTab: React.FC<MemberOverviewTabProps> = ({ memberId }) => {
  const { data: memberDetails, isLoading, error, refetch } = useMemberDetails(memberId)
  const { data: notes } = useMemberNotes(memberId)
  const { data: vettingDetails } = useMemberVetting(memberId)
  const updateRoleMutation = useUpdateMemberRole()
  const { data: validRoles = [] } = useValidRoles()

  // Format roles for MultiSelect
  const roleOptions = formatRolesForSelect(validRoles)

  // Handle role change
  const handleRoleChange = async (selectedRoles: string[]) => {
    try {
      // Backend supports multiple roles as array
      await updateRoleMutation.mutateAsync({
        userId: memberId,
        request: { roles: selectedRoles },
      })

      notifications.show({
        title: 'Success',
        message: 'Role updated successfully',
        color: 'green',
      })

      // Refresh member details to show updated role in page title
      refetch()
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error.message || 'Failed to update role',
        color: 'red',
      })
    }
  }

  if (isLoading) {
    return (
      <Paper p="xl">
        <Text ta="center" c="dimmed">
          Loading member details...
        </Text>
      </Paper>
    )
  }

  if (error) {
    const apiError = error as any
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red" title="Error">
        <Text>Failed to load member details: {(error as Error).message}</Text>
        <Text size="sm" c="dimmed" mt="xs">
          {apiError.response?.status === 403
            ? 'Unable to load member details - authorization pending'
            : 'Please try refreshing the page or contact support if the problem persists.'}
        </Text>
      </Alert>
    )
  }

  if (!memberDetails) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="orange" title="No Data">
        <Text>No member details found.</Text>
      </Alert>
    )
  }

  return (
    <Stack gap="xl">
      {/* Contact Information Section */}
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
          Contact Information
        </Title>
        <Card withBorder p="md" radius="md">
          <Grid>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Scene Name
              </Text>
              <Text fw={500}>{memberDetails.sceneName}</Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Real Name
              </Text>
              <Text fw={500}>{vettingDetails?.realName || 'N/A'}</Text>
            </Grid.Col>
            {vettingDetails?.otherNames && (
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed" mb={4}>
                  Other Names
                </Text>
                <Text fw={500}>{vettingDetails.otherNames}</Text>
              </Grid.Col>
            )}
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Email
              </Text>
              <Text fw={500}>{memberDetails.email || 'N/A'}</Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Phone
              </Text>
              <Text fw={500}>{vettingDetails?.phone || 'N/A'}</Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Discord Name
              </Text>
              <Text fw={500}>{memberDetails.discordName || '-'}</Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                FetLife Handle
              </Text>
              <Text fw={500}>{memberDetails.fetLifeHandle || '-'}</Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Pronouns
              </Text>
              <Text fw={500}>{vettingDetails?.pronouns || 'N/A'}</Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Last Login
              </Text>
              <Text fw={500}>
                {memberDetails.lastLoginAt
                  ? new Date(memberDetails.lastLoginAt).toLocaleDateString()
                  : 'Never'}
              </Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Vetting Status
              </Text>
              <Badge color="purple" variant="light">
                {memberDetails.vettingStatusDisplay}
              </Badge>
            </Grid.Col>
          </Grid>
        </Card>
      </div>

      {/* Role Assignment Section */}
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
        <Card withBorder p="lg" radius="md">
          <MultiSelect
            label="Assigned Roles"
            placeholder="Select roles..."
            data={roleOptions}
            value={memberDetails.role ? memberDetails.role.split(',').filter(r => r.trim()) : []}
            onChange={handleRoleChange}
            searchable
            clearable
            description="Select one or more roles for this member"
          />
        </Card>
      </div>

      {/* Participation Summary */}
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
          Participation Summary
        </Title>
        <Card withBorder p="md" radius="md">
          <Grid>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Events Attended
              </Text>
              <Text size="xl" fw={700} c="burgundy">
                {memberDetails.totalEventsAttended}
              </Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Events Registered
              </Text>
              <Text size="xl" fw={700} c="blue">
                {memberDetails.totalEventsRegistered}
              </Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Active Registrations
              </Text>
              <Text size="xl" fw={700} c="green">
                {memberDetails.activeRegistrations}
              </Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Last Event Attended
              </Text>
              <Text fw={500}>
                {memberDetails.lastEventAttended
                  ? new Date(memberDetails.lastEventAttended).toLocaleDateString()
                  : 'Never'}
              </Text>
            </Grid.Col>
          </Grid>
        </Card>
      </div>

      {/* Notes Section */}
      <MemberNotesSection memberId={memberId} notes={notes || []} />
    </Stack>
  )
}
