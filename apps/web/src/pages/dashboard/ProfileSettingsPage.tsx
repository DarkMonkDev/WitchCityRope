import React, { useState } from 'react'
import {
  Container,
  Title,
  Tabs,
  TextInput,
  Textarea,
  Button,
  Group,
  Stack,
  Box,
  Text,
  Loader,
  Center,
  Alert,
  SimpleGrid,
} from '@mantine/core'
import { Link } from 'react-router-dom'
import { useForm } from '@mantine/form'
import { IconAlertCircle, IconCheck } from '@tabler/icons-react'
import { notifications } from '@mantine/notifications'
import { useProfile, useUpdateProfile, useChangePassword } from '../../hooks/useDashboard'
import type {
  UpdateProfileDto,
  ChangePasswordDto,
  UserProfileDto,
} from '../../types/dashboard.types'

/**
 * Profile Settings Page with 3 tabs
 * - Personal: Scene name, email, pronouns, bio, Discord, FetLife, phone (3-column layout)
 * - Change Password: Change password
 * - Vetting & Membership: Read-only vetting status with membership hold option
 */
export const ProfileSettingsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<string | null>('personal')

  // Fetch profile data using TanStack Query
  const { data: profile, isLoading, error } = useProfile()

  // Show loading state
  if (isLoading) {
    return (
      <Box style={{ background: 'var(--color-cream)', minHeight: '100vh' }} pb="xl">
        <Container size="xl" py="xl">
          <Center py="xl">
            <Stack align="center" gap="md">
              <Loader size="lg" color="burgundy" />
              <Text>Loading profile...</Text>
            </Stack>
          </Center>
        </Container>
      </Box>
    )
  }

  // Show error state
  if (error || !profile) {
    return (
      <Box style={{ background: 'var(--color-cream)', minHeight: '100vh' }} pb="xl">
        <Container size="xl" py="xl">
          <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Profile" mb="lg">
            <Text>
              {error instanceof Error
                ? error.message
                : 'Failed to load your profile. Please try again.'}
            </Text>
          </Alert>
        </Container>
      </Box>
    )
  }

  return (
    <Box style={{ background: 'var(--color-cream)', minHeight: '100vh' }} pb="xl">
      <Container size="xl" py="xl">
        {/* Page Title with View Dashboard Button */}
        <Group justify="space-between" align="center" mb="xl">
          <Title
            order={1}
            tt="uppercase"
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-burgundy)',
              fontSize: '2rem',
            }}
          >
            Profile Settings
          </Title>
          <Button
            component={Link}
            to="/dashboard"
            variant="filled"
            color="burgundy"
            styles={{
              root: {
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
                transition: 'all 0.3s ease',
                height: 'auto',
                paddingTop: '10px',
                paddingBottom: '10px',
                paddingLeft: '20px',
                paddingRight: '20px',
                lineHeight: '1.2',
                display: 'flex',
                alignItems: 'center',
                backgroundColor: 'var(--color-burgundy)',
                color: 'white',
                '&:hover': {
                  borderRadius: '6px 12px 6px 12px',
                  backgroundColor: 'var(--color-burgundy-dark)',
                  transform: 'translateY(-2px)',
                  boxShadow: '0 4px 12px rgba(183, 109, 117, 0.3)',
                },
              },
            }}
          >
            View Dashboard
          </Button>
        </Group>

        {/* Tabs */}
        <Tabs
          value={activeTab}
          onChange={setActiveTab}
          color="burgundy"
          styles={{
            tab: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              fontSize: '16px',
              padding: '12px 24px',
              '&[data-active]': {
                color: 'var(--color-burgundy)',
                borderColor: 'var(--color-burgundy)',
              },
            },
            panel: {
              paddingTop: '24px',
            },
          }}
        >
          <Tabs.List>
            <Tabs.Tab value="personal">Personal</Tabs.Tab>
            <Tabs.Tab value="security">Change Password</Tabs.Tab>
            <Tabs.Tab value="vetting">Vetting & Membership</Tabs.Tab>
          </Tabs.List>

          <Tabs.Panel value="personal">
            <PersonalInfoForm profile={profile} />
          </Tabs.Panel>

          <Tabs.Panel value="security">
            <ChangePasswordForm />
          </Tabs.Panel>

          <Tabs.Panel value="vetting">
            <VettingStatusDisplay profile={profile} />
          </Tabs.Panel>
        </Tabs>
      </Container>
    </Box>
  )
}

// Personal Info Form Component (includes social fields in 3-column layout)
const PersonalInfoForm: React.FC<{ profile: UserProfileDto }> = ({ profile }) => {
  const updateProfileMutation = useUpdateProfile()

  // All fields in one form with 3-column layout
  const form = useForm<UpdateProfileDto>({
    initialValues: {
      sceneName: profile.sceneName,
      firstName: profile.firstName || '',
      lastName: profile.lastName || '',
      email: profile.email,
      pronouns: profile.pronouns || '',
      bio: profile.bio || '',
      discordName: profile.discordName || '',
      fetLifeName: profile.fetLifeName || '',
      phoneNumber: profile.phoneNumber || '',
    },
  })

  const handleSubmit = (values: UpdateProfileDto) => {
    console.log('🔍 PersonalInfoForm - Submitting all fields:', values)
    updateProfileMutation.mutate(values)
  }

  // Handle success/error with useEffect or mutation state
  React.useEffect(() => {
    if (updateProfileMutation.isSuccess) {
      notifications.show({
        title: 'Success',
        message: 'Profile updated successfully',
        color: 'green',
        icon: <IconCheck />,
      })
    }
    if (updateProfileMutation.isError) {
      notifications.show({
        title: 'Error',
        message:
          updateProfileMutation.error instanceof Error
            ? updateProfileMutation.error.message
            : 'Failed to update profile',
        color: 'red',
        icon: <IconAlertCircle />,
      })
    }
  }, [updateProfileMutation.isSuccess, updateProfileMutation.isError, updateProfileMutation.error])

  return (
    <Box
      component="form"
      onSubmit={form.onSubmit(handleSubmit)}
      p="md"
      style={{
        background: 'white',
        borderRadius: '12px',
        border: '1px solid var(--color-taupe)',
      }}
    >
      <Stack gap="md">
        {/* Responsive 3-column layout: 1 col on mobile, 2 on tablet, 3 on desktop */}
        <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="md">
          <TextInput
            label="Scene Name"
            placeholder="Your scene name"
            required
            data-testid="scene-name-input"
            {...form.getInputProps('sceneName')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
          <TextInput
            label="First Name"
            placeholder="Optional"
            data-testid="first-name-input"
            {...form.getInputProps('firstName')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
          <TextInput
            label="Last Name"
            placeholder="Optional"
            data-testid="last-name-input"
            {...form.getInputProps('lastName')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
        </SimpleGrid>

        <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="md">
          <TextInput
            label="Email"
            placeholder="your@email.com"
            type="email"
            required
            data-testid="email-input"
            {...form.getInputProps('email')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
          <TextInput
            label="Pronouns"
            placeholder="e.g., they/them, she/her, he/him"
            data-testid="pronouns-input"
            {...form.getInputProps('pronouns')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
          <TextInput
            label="Discord Username"
            placeholder="Username#1234"
            data-testid="discord-name-input"
            {...form.getInputProps('discordName')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
        </SimpleGrid>

        <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="md">
          <TextInput
            label="FetLife Username"
            placeholder="Your FetLife username"
            data-testid="fetlife-name-input"
            {...form.getInputProps('fetLifeName')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
          <TextInput
            label="Phone Number"
            placeholder="555-123-4567"
            type="tel"
            data-testid="phone-number-input"
            {...form.getInputProps('phoneNumber')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
          {/* Empty third column on desktop */}
          <Box />
        </SimpleGrid>

        <Textarea
          label="Bio"
          placeholder="Tell us about yourself..."
          rows={4}
          maxLength={2000}
          data-testid="bio-input"
          {...form.getInputProps('bio')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <Group justify="flex-end">
          <Button
            type="submit"
            color="burgundy"
            loading={updateProfileMutation.isPending}
            styles={{
              root: {
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                textTransform: 'uppercase',
                height: 'auto',
                paddingTop: '12px',
                paddingBottom: '12px',
                paddingLeft: '24px',
                paddingRight: '24px',
                lineHeight: '1.2',
                display: 'flex',
                alignItems: 'center',
              },
            }}
          >
            Save Changes
          </Button>
        </Group>
      </Stack>
    </Box>
  )
}

// Change Password Form Component
const ChangePasswordForm: React.FC = () => {
  const changePasswordMutation = useChangePassword()

  const form = useForm<ChangePasswordDto>({
    initialValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
    validate: {
      newPassword: (value) => {
        if (value.length < 8) {
          return 'Password must be at least 8 characters'
        }
        if (!/(?=.*[a-z])/.test(value)) {
          return 'Password must contain lowercase letter'
        }
        if (!/(?=.*[A-Z])/.test(value)) {
          return 'Password must contain uppercase letter'
        }
        if (!/(?=.*\d)/.test(value)) {
          return 'Password must contain number'
        }
        return null
      },
      confirmPassword: (value, values) =>
        value !== values.newPassword ? 'Passwords do not match' : null,
    },
  })

  const handleSubmit = (values: ChangePasswordDto) => {
    changePasswordMutation.mutate(values)
  }

  // Handle success/error
  React.useEffect(() => {
    if (changePasswordMutation.isSuccess) {
      notifications.show({
        title: 'Success',
        message: 'Password changed successfully',
        color: 'green',
        icon: <IconCheck />,
      })
      form.reset()
    }
    if (changePasswordMutation.isError) {
      notifications.show({
        title: 'Error',
        message:
          changePasswordMutation.error instanceof Error
            ? changePasswordMutation.error.message
            : 'Failed to change password',
        color: 'red',
        icon: <IconAlertCircle />,
      })
    }
  }, [
    changePasswordMutation.isSuccess,
    changePasswordMutation.isError,
    changePasswordMutation.error,
    form,
  ])

  return (
    <Box
      component="form"
      onSubmit={form.onSubmit(handleSubmit)}
      p="md"
      style={{
        background: 'white',
        borderRadius: '12px',
        border: '1px solid var(--color-taupe)',
      }}
    >
      <Stack gap="md">
        {/* Responsive 3-column layout: 1 col on mobile, 2 on tablet, 3 on desktop */}
        <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="md">
          <TextInput
            label="Current Password"
            type="password"
            required
            {...form.getInputProps('currentPassword')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />

          <TextInput
            label="New Password"
            type="password"
            required
            {...form.getInputProps('newPassword')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />

          <TextInput
            label="Confirm New Password"
            type="password"
            required
            {...form.getInputProps('confirmPassword')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
        </SimpleGrid>

        <Text size="sm" c="dimmed">
          Password must be at least 8 characters and contain uppercase, lowercase, and number.
        </Text>

        <Group justify="flex-end">
          <Button
            type="submit"
            color="burgundy"
            loading={changePasswordMutation.isPending}
            styles={{
              root: {
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                textTransform: 'uppercase',
                height: 'auto',
                paddingTop: '12px',
                paddingBottom: '12px',
                paddingLeft: '24px',
                paddingRight: '24px',
                lineHeight: '1.2',
                display: 'flex',
                alignItems: 'center',
              },
            }}
          >
            Change Password
          </Button>
        </Group>
      </Stack>
    </Box>
  )
}

// Vetting Status Display Component
const VettingStatusDisplay: React.FC<{ profile: UserProfileDto }> = ({ profile }) => {
  return (
    <Box
      p="md"
      style={{
        background: 'white',
        borderRadius: '12px',
        border: '1px solid var(--color-taupe)',
      }}
    >
      <Stack gap="md">
        <TextInput
          label="Vetting Status"
          value={profile.vettingStatus}
          readOnly
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
            input: {
              backgroundColor: 'var(--color-cream)',
              color: 'var(--color-charcoal)',
            },
          }}
        />

        <Text size="sm" c="dimmed">
          Your vetting status is managed by administrators. If you have questions about your status,
          please contact us.
        </Text>

        <Group justify="flex-end">
          <Button
            variant="outline"
            color="burgundy"
            styles={{
              root: {
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                textTransform: 'uppercase',
                height: 'auto',
                paddingTop: '12px',
                paddingBottom: '12px',
                paddingLeft: '24px',
                paddingRight: '24px',
                lineHeight: '1.2',
                display: 'flex',
                alignItems: 'center',
              },
            }}
            onClick={() => {
              notifications.show({
                title: 'Coming Soon',
                message: 'Membership hold functionality will be available soon',
                color: 'blue',
              })
            }}
          >
            Put Membership On Hold
          </Button>
        </Group>
      </Stack>
    </Box>
  )
}
