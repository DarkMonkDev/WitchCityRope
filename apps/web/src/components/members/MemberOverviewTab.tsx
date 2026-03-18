import { useState } from 'react'
import { Stack, Title, Card, Text, Group, Badge, Grid, Paper, Alert, MultiSelect, Anchor, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { IconAlertCircle } from '@tabler/icons-react'
import { useMemberDetails, useMemberNotes, useUpdateMemberRole, useCreateMemberNote, useUpdateMemberContactInfo } from '../../lib/api/hooks/useMemberDetails'
import { NotesSection } from '../notes/NotesSection'
import { MemberNoteRenderer } from '../notes/MemberNoteRenderer'
import { WCRButton } from '../ui/WCRButton'
import { ResetPasswordSection } from './ResetPasswordSection'
import { notifications } from '@mantine/notifications'
import { useValidRoles, formatRolesForSelect } from '../../lib/api/hooks/useValidRoles'

interface MemberOverviewTabProps {
  memberId: string
}

export const MemberOverviewTab: React.FC<MemberOverviewTabProps> = ({ memberId }) => {
  const { data: memberDetails, isLoading, error, refetch } = useMemberDetails(memberId)
  const { data: notes, refetch: refetchNotes } = useMemberNotes(memberId)
  const updateRoleMutation = useUpdateMemberRole()
  const createNoteMutation = useCreateMemberNote()
  const { data: validRoles = [] } = useValidRoles()
  const [isEditing, setIsEditing] = useState(false)
  const updateContactMutation = useUpdateMemberContactInfo()

  const contactForm = useForm({
    initialValues: {
      sceneName: '',
      firstName: '',
      lastName: '',
      otherNames: '',
      email: '',
      phoneNumber: '',
      discordName: '',
      fetLifeName: '',
      pronouns: '',
    },
    validate: {
      sceneName: (value) => {
        if (!value || value.trim().length === 0) return 'Scene name is required'
        if (value.trim().length < 3) return 'Scene name must be at least 3 characters'
        if (value.length > 50) return 'Scene name cannot exceed 50 characters'
        return null
      },
      email: (value) => {
        if (!value || value.trim().length === 0) return 'Email is required'
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return 'Invalid email format'
        return null
      },
      firstName: (value) => value && value.length > 50 ? 'Cannot exceed 50 characters' : null,
      lastName: (value) => value && value.length > 50 ? 'Cannot exceed 50 characters' : null,
      otherNames: (value) => value && value.length > 500 ? 'Cannot exceed 500 characters' : null,
      phoneNumber: (value) => value && value.length > 20 ? 'Cannot exceed 20 characters' : null,
      discordName: (value) => value && value.length > 100 ? 'Cannot exceed 100 characters' : null,
      fetLifeName: (value) => value && value.length > 100 ? 'Cannot exceed 100 characters' : null,
      pronouns: (value) => value && value.length > 50 ? 'Cannot exceed 50 characters' : null,
    },
  })

  /** Enter edit mode - populate form with current member data from User entity */
  const handleEditClick = () => {
    contactForm.setValues({
      sceneName: memberDetails?.sceneName || '',
      firstName: memberDetails?.firstName || '',
      lastName: memberDetails?.lastName || '',
      otherNames: memberDetails?.otherNames || '',
      email: memberDetails?.email || '',
      phoneNumber: memberDetails?.phoneNumber || '',
      discordName: memberDetails?.discordName || '',
      fetLifeName: memberDetails?.fetLifeHandle || '',
      pronouns: memberDetails?.pronouns || '',
    })
    contactForm.resetDirty()
    setIsEditing(true)
  }

  /** Cancel editing - reset form and return to view mode */
  const handleCancelEdit = () => {
    contactForm.reset()
    setIsEditing(false)
  }

  /** Save contact info changes via admin endpoint */
  const handleSaveContactInfo = async (values: typeof contactForm.values) => {
    try {
      await updateContactMutation.mutateAsync({
        userId: memberId,
        request: {
          sceneName: values.sceneName,
          firstName: values.firstName || null,
          lastName: values.lastName || null,
          email: values.email,
          pronouns: values.pronouns || null,
          discordName: values.discordName || null,
          fetLifeName: values.fetLifeName || null,
          phoneNumber: values.phoneNumber || null,
          otherNames: values.otherNames || null,
        },
      })

      notifications.show({
        title: 'Success',
        message: 'Contact information updated successfully',
        color: 'green',
      })
      setIsEditing(false)
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error?.response?.data?.detail || error.message || 'Failed to update contact information',
        color: 'red',
      })
    }
  }

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
        <form onSubmit={contactForm.onSubmit(handleSaveContactInfo)}>
          {/* Title with Edit button */}
          <Group
            justify="space-between"
            align="center"
            mb="md"
            style={{
              borderBottom: '2px solid var(--mantine-color-burgundy-3)',
              paddingBottom: '8px',
            }}
          >
            <Title order={2} c="burgundy">
              Contact Information
            </Title>
            {!isEditing && (
              <WCRButton variant="outline" size="compact-sm" onClick={handleEditClick}>
                Edit
              </WCRButton>
            )}
          </Group>
          <Card withBorder p="md" radius="md">
            <Grid>
              {/* Scene Name */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                {isEditing ? (
                  <TextInput label="Scene Name" required {...contactForm.getInputProps('sceneName')} />
                ) : (
                  <Group gap="xs" wrap="nowrap">
                    <Text size="sm" c="dimmed">Scene Name:</Text>
                    <Text fw={500}>{memberDetails.sceneName}</Text>
                  </Group>
                )}
              </Grid.Col>

              {/* Name (First + Last) */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                {isEditing ? (
                  <TextInput label="First Name" {...contactForm.getInputProps('firstName')} />
                ) : (
                  <Group gap="xs" wrap="nowrap">
                    <Text size="sm" c="dimmed">Name:</Text>
                    <Text fw={500}>{[memberDetails.firstName, memberDetails.lastName].filter(Boolean).join(' ') || '-'}</Text>
                  </Group>
                )}
              </Grid.Col>

              {/* Last Name - only shown in edit mode as separate field */}
              {isEditing && (
                <Grid.Col span={{ base: 6, md: 4 }}>
                  <TextInput label="Last Name" {...contactForm.getInputProps('lastName')} />
                </Grid.Col>
              )}

              {/* Other Names */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                {isEditing ? (
                  <TextInput label="Other Names" {...contactForm.getInputProps('otherNames')} />
                ) : (
                  <Group gap="xs" wrap="nowrap">
                    <Text size="sm" c="dimmed">Other Names:</Text>
                    <Text fw={500}>{memberDetails.otherNames || '-'}</Text>
                  </Group>
                )}
              </Grid.Col>

              {/* Email */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                {isEditing ? (
                  <TextInput label="Email" required {...contactForm.getInputProps('email')} />
                ) : (
                  <Group gap="xs" wrap="nowrap">
                    <Text size="sm" c="dimmed">Email:</Text>
                    <Text fw={500}>{memberDetails.email || '-'}</Text>
                  </Group>
                )}
              </Grid.Col>

              {/* Phone */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                {isEditing ? (
                  <TextInput label="Phone" {...contactForm.getInputProps('phoneNumber')} />
                ) : (
                  <Group gap="xs" wrap="nowrap">
                    <Text size="sm" c="dimmed">Phone:</Text>
                    <Text fw={500}>{memberDetails.phoneNumber || '-'}</Text>
                  </Group>
                )}
              </Grid.Col>

              {/* Discord Name */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                {isEditing ? (
                  <TextInput label="Discord Name" {...contactForm.getInputProps('discordName')} />
                ) : (
                  <Group gap="xs" wrap="nowrap">
                    <Text size="sm" c="dimmed">Discord Name:</Text>
                    <Text fw={500}>{memberDetails.discordName || '-'}</Text>
                  </Group>
                )}
              </Grid.Col>

              {/* FetLife Handle */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                {isEditing ? (
                  <TextInput label="FetLife Handle" {...contactForm.getInputProps('fetLifeName')} />
                ) : (
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
                )}
              </Grid.Col>

              {/* Pronouns */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                {isEditing ? (
                  <TextInput label="Pronouns" {...contactForm.getInputProps('pronouns')} />
                ) : (
                  <Group gap="xs" wrap="nowrap">
                    <Text size="sm" c="dimmed">Pronouns:</Text>
                    <Text fw={500}>{memberDetails.pronouns || '-'}</Text>
                  </Group>
                )}
              </Grid.Col>

              {/* Last Login - always read-only */}
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

              {/* Vetting Status - always read-only */}
              <Grid.Col span={{ base: 6, md: 4 }}>
                <Group gap="xs" wrap="nowrap">
                  <Text size="sm" c="dimmed">Vetting Status:</Text>
                  <Badge color="purple" variant="light">
                    {memberDetails.vettingStatusDisplay}
                  </Badge>
                </Group>
              </Grid.Col>
            </Grid>

            {/* Save/Cancel buttons - only visible in edit mode */}
            {isEditing && (
              <Group justify="flex-end" mt="md">
                <WCRButton
                  variant="outline"
                  size="sm"
                  onClick={handleCancelEdit}
                  disabled={updateContactMutation.isPending}
                >
                  Cancel
                </WCRButton>
                <WCRButton
                  variant="secondary"
                  size="sm"
                  type="submit"
                  loading={updateContactMutation.isPending}
                >
                  {updateContactMutation.isPending ? 'Saving...' : 'Save Changes'}
                </WCRButton>
              </Group>
            )}
          </Card>
        </form>
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

      {/* Reset Password Section */}
      <ResetPasswordSection memberId={memberId} sceneName={memberDetails.sceneName ?? ''} />

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
