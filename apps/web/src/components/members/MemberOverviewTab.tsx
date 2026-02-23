import { Stack, Title, Card, Text, Group, Badge, Grid, Paper, Alert, MultiSelect, Anchor } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useMemberDetails, useMemberNotes, useMemberVetting, useUpdateMemberRole, useCreateMemberNote } from '../../lib/api/hooks/useMemberDetails'
import { NotesSection } from '../notes/NotesSection'
import { MemberNoteRenderer } from '../notes/MemberNoteRenderer'
import { notifications } from '@mantine/notifications'
import { useValidRoles, formatRolesForSelect } from '../../lib/api/hooks/useValidRoles'

interface MemberOverviewTabProps {
  memberId: string
}

export const MemberOverviewTab: React.FC<MemberOverviewTabProps> = ({ memberId }) => {
  const { data: memberDetails, isLoading, error, refetch } = useMemberDetails(memberId)
  const { data: notes, refetch: refetchNotes } = useMemberNotes(memberId)
  const { data: vettingDetails } = useMemberVetting(memberId)
  const updateRoleMutation = useUpdateMemberRole()
  const createNoteMutation = useCreateMemberNote()
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

  // Handle save note
  const handleSaveNote = async (content: string) => {
    try {
      await createNoteMutation.mutateAsync({
        userId: memberId,
        request: {
          content,
        },
      })

      notifications.show({
        title: 'Success',
        message: 'Note added successfully',
        color: 'green',
      })

      // Refresh notes list
      refetchNotes()
    } catch (error) {
      notifications.show({
        title: 'Error',
        message: 'Failed to add note. Please try again.',
        color: 'red',
      })
      throw error // Re-throw to let NotesSection handle loading state
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
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Scene Name:</Text>
                <Text fw={500}>{memberDetails.sceneName}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Name:</Text>
                <Text fw={500}>{[vettingDetails?.firstName, vettingDetails?.lastName].filter(Boolean).join(' ') || '-'}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Other Names:</Text>
                <Text fw={500}>{vettingDetails?.otherNames || '-'}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Email:</Text>
                <Text fw={500}>{memberDetails.email || '-'}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Phone:</Text>
                <Text fw={500}>{vettingDetails?.phone || '-'}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Discord Name:</Text>
                <Text fw={500}>{memberDetails.discordName || '-'}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">FetLife Handle:</Text>
                {memberDetails.fetLifeHandle ? (
                  <Anchor
                    href={`https://fetlife.com/${memberDetails.fetLifeHandle.trim()}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    fw={500}
                  >
                    {memberDetails.fetLifeHandle.trim()}
                  </Anchor>
                ) : (
                  <Text fw={500}>-</Text>
                )}
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Pronouns:</Text>
                <Text fw={500}>{vettingDetails?.pronouns || '-'}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Last Login:</Text>
                <Text fw={500}>
                  {memberDetails.lastLoginAt
                    ? new Date(memberDetails.lastLoginAt).toLocaleDateString()
                    : 'Never'}
                </Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Vetting Status:</Text>
                <Badge color="purple" variant="light">
                  {memberDetails.vettingStatusDisplay}
                </Badge>
              </Group>
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
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Events Attended:</Text>
                <Text fw={500} c="burgundy">{memberDetails.totalEventsAttended}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Last Event Attended:</Text>
                <Text fw={500}>
                  {memberDetails.lastEventAttended && new Date(memberDetails.lastEventAttended).getFullYear() > 1900
                    ? new Date(memberDetails.lastEventAttended).toLocaleDateString()
                    : '-'}
                </Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Future Events:</Text>
                <Text fw={500} c="blue">{memberDetails.futureEvents ?? 0}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Total Past Events Registered:</Text>
                <Text fw={500}>{memberDetails.totalPastEventsRegistered ?? 0}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">Canceled:</Text>
                <Text fw={500} c="orange">{memberDetails.cancelledRegistrations ?? 0}</Text>
              </Group>
            </Grid.Col>
            <Grid.Col span={{ base: 6, md: 4 }}>
              <Group gap="xs" wrap="nowrap">
                <Text size="sm" c="dimmed">No-Show:</Text>
                <Text fw={500} c="red">{memberDetails.noShows ?? 0}</Text>
              </Group>
            </Grid.Col>
          </Grid>
        </Card>
      </div>

      {/* Notes Section */}
      <NotesSection
        notes={notes || []}
        onSaveNote={handleSaveNote}
        renderFullNote={MemberNoteRenderer}
        placeholder="Add a note about this member..."
        title="Notes & Vetting History"
      />
    </Stack>
  )
}
