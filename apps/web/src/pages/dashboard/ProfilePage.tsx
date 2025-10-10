import React from 'react'
import { Box, Title, Text, Paper, TextInput, Group, Stack, Alert, Loader } from '@mantine/core'
import { useForm } from '@mantine/form'
import { useCurrentUser } from '../../features/auth/api/queries'
import { DashboardLayout } from '../../components/dashboard/DashboardLayout'
import type { UserDto } from '@witchcityrope/shared-types'

/**
 * Profile Page - User profile management with API integration
 * Uses real API data via TanStack Query hooks
 */
export const ProfilePage: React.FC = () => {
  const { data: user, isLoading, error } = useCurrentUser()

  // Form for profile updates - prepopulated with user data
  const profileForm = useForm<Partial<UserDto>>({
    initialValues: {
      sceneName: user?.sceneName || '',
      email: user?.email || '',
      // Add other user fields as they become available from the API
    },
    validate: {
      sceneName: (value) => {
        if (!value || value.length < 2) return 'Scene name must be at least 2 characters'
        if (value.length > 50) return 'Scene name must be less than 50 characters'
        return null
      },
      email: (value) => {
        if (!value) return 'Email is required'
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return 'Invalid email format'
        return null
      },
    },
  })

  // Update form when user data loads
  React.useEffect(() => {
    if (user) {
      profileForm.setValues({
        sceneName: user.sceneName || '',
        email: user.email || '',
      })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user])

  const handleProfileSubmit = (values: Partial<UserDto>) => {
    // TODO: Implement profile update API call using mutation
    console.log('Profile update submitted:', values)
    // For now, just show the data that would be submitted
  }

  if (isLoading) {
    return (
      <DashboardLayout>
        <Box>
          <Title
            order={1}
            mb="xl"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '28px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Profile
          </Title>

          <Box style={{ textAlign: 'center', padding: '40px' }}>
            <Loader size="lg" color="#880124" data-testid="profile-loader" />
            <Text style={{ marginTop: '16px', color: '#8B8680' }}>Loading your profile...</Text>
          </Box>
        </Box>
      </DashboardLayout>
    )
  }

  if (error) {
    return (
      <DashboardLayout>
        <Box>
          <Title
            order={1}
            mb="xl"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '28px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Profile
          </Title>

          <Alert
            color="red"
            style={{
              background: 'rgba(220, 20, 60, 0.1)',
              border: '1px solid rgba(220, 20, 60, 0.3)',
            }}
          >
            Failed to load your profile. Please try refreshing the page.
          </Alert>
        </Box>
      </DashboardLayout>
    )
  }

  return (
    <DashboardLayout>
      <Box>
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '28px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Profile
        </Title>

        {/* Note about future functionality */}
        <Alert
          color="blue"
          mb="xl"
          style={{
            background: 'rgba(59, 130, 246, 0.1)',
            border: '1px solid rgba(59, 130, 246, 0.3)',
            borderRadius: '6px',
          }}
        >
          <Text style={{ fontSize: '14px' }}>
            <strong>Note:</strong> Profile updates are not yet connected to the API. Form validation
            is active, but changes won't be saved until the update mutation is implemented.
          </Text>
        </Alert>

        <Stack gap="lg">
          {/* Profile Information Section */}
          <Paper
            style={{
              background: '#FFF8F0',
              padding: '24px',
              transition: 'all 0.3s ease',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.boxShadow = '0 2px 12px rgba(0,0,0,0.05)'
              e.currentTarget.style.transform = 'translateX(2px)'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.boxShadow = 'none'
              e.currentTarget.style.transform = 'translateX(0)'
            }}
          >
            <Title
              order={2}
              mb="lg"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Profile Information
            </Title>

            <form onSubmit={profileForm.onSubmit(handleProfileSubmit)}>
              <Stack gap="md">
                <Group grow>
                  <TextInput
                    id="scene-name-input"
                    label="Scene Name"
                    placeholder="Your community name"
                    required
                    styles={{
                      label: {
                        fontFamily: "'Montserrat', sans-serif",
                        fontWeight: 600,
                        color: '#2B2B2B',
                        fontSize: '14px',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                        marginBottom: '8px',
                      },
                      input: {
                        fontFamily: "'Source Sans 3', sans-serif",
                        fontSize: '16px',
                        border: '1px solid rgba(183, 109, 117, 0.3)',
                        borderRadius: '6px',
                        background: '#FAF6F2',
                        '&:focus': {
                          borderColor: '#880124',
                          boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)',
                        },
                      },
                    }}
                    {...profileForm.getInputProps('sceneName')}
                  />

                  <TextInput
                    id="email-address-input"
                    label="Email Address"
                    placeholder="your.email@example.com"
                    type="email"
                    required
                    styles={{
                      label: {
                        fontFamily: "'Montserrat', sans-serif",
                        fontWeight: 600,
                        color: '#2B2B2B',
                        fontSize: '14px',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                        marginBottom: '8px',
                      },
                      input: {
                        fontFamily: "'Source Sans 3', sans-serif",
                        fontSize: '16px',
                        border: '1px solid rgba(183, 109, 117, 0.3)',
                        borderRadius: '6px',
                        background: '#FAF6F2',
                        '&:focus': {
                          borderColor: '#880124',
                          boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)',
                        },
                      },
                    }}
                    {...profileForm.getInputProps('email')}
                  />
                </Group>

                <button
                  type="submit"
                  className="btn btn-primary"
                  style={{ alignSelf: 'flex-start' }}
                >
                  Update Profile
                </button>
              </Stack>
            </form>
          </Paper>

          {/* Account Information Section - Read Only */}
          <Paper
            style={{
              background: '#FFF8F0',
              padding: '24px',
              transition: 'all 0.3s ease',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.boxShadow = '0 2px 12px rgba(0,0,0,0.05)'
              e.currentTarget.style.transform = 'translateX(2px)'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.boxShadow = 'none'
              e.currentTarget.style.transform = 'translateX(0)'
            }}
          >
            <Title
              order={2}
              mb="lg"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Account Information
            </Title>

            <Stack gap="md">
              <Box
                style={{
                  background: 'rgba(183, 109, 117, 0.05)',
                  padding: '16px',
                  borderRadius: '6px',
                  border: '1px solid rgba(183, 109, 117, 0.2)',
                }}
              >
                <Group>
                  <Box style={{ flex: 1 }}>
                    <Text
                      style={{
                        fontFamily: "'Montserrat', sans-serif",
                        fontWeight: 600,
                        fontSize: '14px',
                        color: '#2B2B2B',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                        marginBottom: '4px',
                      }}
                    >
                      User ID
                    </Text>
                    <Text
                      style={{
                        fontFamily: 'monospace',
                        fontSize: '16px',
                        color: '#8B8680',
                      }}
                    >
                      {user?.id || 'Not available'}
                    </Text>
                  </Box>

                  <Box style={{ flex: 1 }}>
                    <Text
                      style={{
                        fontFamily: "'Montserrat', sans-serif",
                        fontWeight: 600,
                        fontSize: '14px',
                        color: '#2B2B2B',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                        marginBottom: '4px',
                      }}
                    >
                      Account Created
                    </Text>
                    <Text
                      style={{
                        fontSize: '16px',
                        color: '#8B8680',
                      }}
                    >
                      {user?.createdAt
                        ? new Date(user.createdAt).toLocaleDateString('en-US', {
                            year: 'numeric',
                            month: 'long',
                            day: 'numeric',
                          })
                        : 'Not available'}
                    </Text>
                  </Box>
                </Group>
              </Box>

              {/* TODO: Fix UserDto type resolution to include lastLoginAt
              {user?.lastLoginAt && (
                <Box
                  style={{
                    background: 'rgba(183, 109, 117, 0.05)',
                    padding: '16px',
                    borderRadius: '6px',
                    border: '1px solid rgba(183, 109, 117, 0.2)',
                  }}
                >
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 600,
                      fontSize: '14px',
                      color: '#2B2B2B',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      marginBottom: '4px',
                    }}
                  >
                    Last Login
                  </Text>
                  <Text
                    style={{
                      fontSize: '16px',
                      color: '#8B8680',
                    }}
                  >
                    {new Date(user.lastLoginAt!).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric',
                      hour: 'numeric',
                      minute: '2-digit',
                      hour12: true
                    })}
                  </Text>
                </Box>
              )}
              */}
            </Stack>
          </Paper>

          {/* Community Guidelines Section */}
          <Paper
            style={{
              background: '#FFF8F0',
              padding: '24px',
              transition: 'all 0.3s ease',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.boxShadow = '0 2px 12px rgba(0,0,0,0.05)'
              e.currentTarget.style.transform = 'translateX(2px)'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.boxShadow = 'none'
              e.currentTarget.style.transform = 'translateX(0)'
            }}
          >
            <Title
              order={2}
              mb="md"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Community Guidelines
            </Title>

            <Box
              style={{
                background: 'rgba(183, 109, 117, 0.05)',
                padding: '20px',
                borderRadius: '6px',
                border: '1px solid rgba(183, 109, 117, 0.2)',
              }}
            >
              <Text
                style={{
                  fontSize: '15px',
                  lineHeight: 1.6,
                  color: '#4A4A4A',
                  marginBottom: '16px',
                }}
              >
                Your scene name is how other community members will know you. Please choose
                something that:
              </Text>

              <Box component="ul" style={{ margin: 0, paddingLeft: '20px', color: '#4A4A4A' }}>
                <li style={{ marginBottom: '8px', fontSize: '14px' }}>
                  Represents you authentically in the rope community
                </li>
                <li style={{ marginBottom: '8px', fontSize: '14px' }}>
                  Respects our community values and guidelines
                </li>
                <li style={{ marginBottom: '8px', fontSize: '14px' }}>
                  Is appropriate for all community events and interactions
                </li>
                <li style={{ marginBottom: '8px', fontSize: '14px' }}>
                  Does not impersonate others or organizations
                </li>
              </Box>

              <Text
                style={{
                  fontSize: '13px',
                  color: '#8B8680',
                  marginTop: '16px',
                  fontStyle: 'italic',
                }}
              >
                Changes to your scene name may require approval from community moderators.
              </Text>
            </Box>
          </Paper>
        </Stack>
      </Box>
    </DashboardLayout>
  )
}
