import React, { useState } from 'react'
import { Box, Title, Text, Paper, Group, Stack, Switch, Alert } from '@mantine/core'
import { useForm } from '@mantine/form'
import { MantinePasswordInput } from '../../components/forms/MantineFormInputs'
import { DashboardLayout } from '../../components/dashboard/DashboardLayout'

/**
 * Security Settings Page
 * Provides password change, 2FA setup, and basic privacy settings
 * Follows the simplified design requirements
 */
export const SecurityPage: React.FC = () => {
  const [is2FAEnabled, setIs2FAEnabled] = useState(true)
  const [profileVisibility, setProfileVisibility] = useState(true)
  const [eventVisibility, setEventVisibility] = useState(false)
  const [contactVisibility, setContactVisibility] = useState(false)

  const passwordForm = useForm({
    initialValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
    validate: {
      currentPassword: (value) => (value.length < 1 ? 'Current password is required' : null),
      newPassword: (value) => {
        if (value.length < 8) return 'Password must be at least 8 characters'
        if (!/(?=.*[a-z])(?=.*[A-Z])/.test(value))
          return 'Password must contain uppercase and lowercase letters'
        if (!/(?=.*\d)/.test(value)) return 'Password must contain at least one number'
        if (!/(?=.*[!@#$%^&*])/.test(value))
          return 'Password must contain at least one special character'
        return null
      },
      confirmPassword: (value, values) =>
        value !== values.newPassword ? 'Passwords do not match' : null,
    },
  })

  const handlePasswordSubmit = (values: typeof passwordForm.values) => {
    // TODO: Implement password change API call using useMutation hook
    // Example: useChangePassword() mutation that calls /api/auth/change-password
    console.log('Password change submitted:', {
      currentPassword: values.currentPassword,
      newPassword: values.newPassword,
    })
    // Reset form after successful submission
    passwordForm.reset()
  }

  const handleDisable2FA = () => {
    // TODO: Implement 2FA disable API call using useMutation hook
    // Example: useDisable2FA() mutation that calls /api/auth/disable-2fa
    console.log('Disabling 2FA...')
    setIs2FAEnabled(false)
  }

  const handleDataDownload = () => {
    // TODO: Implement data download API call using useMutation hook
    // Example: useRequestDataDownload() mutation that calls /api/auth/request-data-download
    console.log('Requesting data download...')
  }

  return (
    <DashboardLayout>
      {/* Page Header */}
      <Box mb="xl">
        <Title
          order={1}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '28px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Security Settings
        </Title>
      </Box>

      <Stack gap="lg">
        {/* Change Password Section */}
        <Paper
          style={{
            background: '#FFF8F0',
            padding: '16px',
            transition: 'all 0.3s ease',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.boxShadow = '0 1px 8px rgba(0,0,0,0.05)'
            e.currentTarget.style.transform = 'translateX(2px)'
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.boxShadow = 'none'
            e.currentTarget.style.transform = 'translateX(0)'
          }}
        >
          <Box mb="lg">
            <Title
              order={2}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Change Password
            </Title>
          </Box>

          <form onSubmit={passwordForm.onSubmit(handlePasswordSubmit)}>
            <Stack gap="md">
              <MantinePasswordInput
                label="Current Password"
                placeholder="Enter your current password"
                required
                taperedUnderline={true}
                showStrengthMeter={false}
                {...passwordForm.getInputProps('currentPassword')}
              />

              <Group grow>
                <MantinePasswordInput
                  label="New Password"
                  placeholder="Enter new password"
                  required
                  taperedUnderline={true}
                  showStrengthMeter={true}
                  {...passwordForm.getInputProps('newPassword')}
                />

                <MantinePasswordInput
                  label="Confirm New Password"
                  placeholder="Confirm new password"
                  required
                  taperedUnderline={true}
                  showStrengthMeter={false}
                  {...passwordForm.getInputProps('confirmPassword')}
                />
              </Group>

              <button type="submit" className="btn btn-secondary">
                Update Password
              </button>

              {/* Password Requirements */}
              <Paper
                style={{
                  background: 'rgba(183, 109, 117, 0.03)',
                  border: '1px solid rgba(183, 109, 117, 0.2)',
                  borderRadius: '6px',
                  padding: '16px',
                  marginTop: '16px',
                }}
              >
                <Title
                  order={4}
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontWeight: 700,
                    color: '#880124',
                    marginBottom: '8px',
                    fontSize: '14px',
                    textTransform: 'uppercase',
                  }}
                >
                  Password Requirements:
                </Title>
                <Box
                  component="ul"
                  style={{ margin: 0, paddingLeft: '16px', color: '#4A4A4A', fontSize: '14px' }}
                >
                  <li style={{ marginBottom: '4px' }}>At least 8 characters long</li>
                  <li style={{ marginBottom: '4px' }}>Include uppercase and lowercase letters</li>
                  <li style={{ marginBottom: '4px' }}>Include at least one number</li>
                  <li style={{ marginBottom: '4px' }}>
                    Include at least one special character (!@#$%^&*)
                  </li>
                </Box>
              </Paper>
            </Stack>
          </form>
        </Paper>

        {/* Two-Factor Authentication Section */}
        <Paper
          style={{
            background: '#FFF8F0',
            padding: '16px',
            transition: 'all 0.3s ease',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.boxShadow = '0 1px 8px rgba(0,0,0,0.05)'
            e.currentTarget.style.transform = 'translateX(2px)'
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.boxShadow = 'none'
            e.currentTarget.style.transform = 'translateX(0)'
          }}
        >
          <Box mb="lg">
            <Title
              order={2}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Two-Factor Authentication
            </Title>
          </Box>

          {is2FAEnabled ? (
            <>
              <Alert
                icon="✓"
                color="green"
                style={{
                  background: 'rgba(34, 139, 34, 0.03)',
                  border: '1px solid rgba(34, 139, 34, 0.2)',
                  borderRadius: '6px',
                  marginBottom: '16px',
                }}
              >
                <Title order={3} style={{ fontSize: '16px', fontWeight: 700, marginBottom: '4px' }}>
                  Two-Factor Authentication Enabled
                </Title>
                <Text style={{ fontSize: '14px', color: '#8B8680' }}>
                  Your account is protected with 2FA using your authenticator app
                </Text>
              </Alert>

              <button
                onClick={handleDisable2FA}
                className="btn btn-secondary"
                style={{
                  borderColor: '#DC143C',
                  color: '#DC143C',
                }}
              >
                Disable 2FA
              </button>
            </>
          ) : (
            <Alert color="yellow" style={{ marginBottom: '16px' }}>
              <Text>Two-Factor Authentication is disabled. Enable it for better security.</Text>
            </Alert>
          )}
        </Paper>

        {/* Privacy Settings Section */}
        <Paper
          style={{
            background: '#FFF8F0',
            padding: '16px',
            transition: 'all 0.3s ease',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.boxShadow = '0 1px 8px rgba(0,0,0,0.05)'
            e.currentTarget.style.transform = 'translateX(2px)'
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.boxShadow = 'none'
            e.currentTarget.style.transform = 'translateX(0)'
          }}
        >
          <Box mb="lg">
            <Title
              order={2}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Privacy Settings
            </Title>
          </Box>

          <Stack gap="md">
            <Paper
              style={{
                background: '#FAF6F2',
                padding: '16px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                transition: 'all 0.3s ease',
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = 'translateX(2px)'
                e.currentTarget.style.boxShadow = '0 1px 6px rgba(0,0,0,0.05)'
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = 'translateX(0)'
                e.currentTarget.style.boxShadow = 'none'
              }}
            >
              <Box>
                <Title order={4} style={{ fontSize: '15px', fontWeight: 600, marginBottom: '4px' }}>
                  Profile Visibility
                </Title>
                <Text style={{ color: '#8B8680', fontSize: '13px' }}>
                  Make your profile visible to other community members
                </Text>
              </Box>
              <Switch
                checked={profileVisibility}
                onChange={(event) => setProfileVisibility(event.currentTarget.checked)}
                color="green"
                aria-label="Profile Visibility"
              />
            </Paper>

            <Paper
              style={{
                background: '#FAF6F2',
                padding: '16px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                transition: 'all 0.3s ease',
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = 'translateX(2px)'
                e.currentTarget.style.boxShadow = '0 1px 6px rgba(0,0,0,0.05)'
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = 'translateX(0)'
                e.currentTarget.style.boxShadow = 'none'
              }}
            >
              <Box>
                <Title order={4} style={{ fontSize: '15px', fontWeight: 600, marginBottom: '4px' }}>
                  Event Attendance Visibility
                </Title>
                <Text style={{ color: '#8B8680', fontSize: '13px' }}>
                  Show which events you're attending to other members
                </Text>
              </Box>
              <Switch
                checked={eventVisibility}
                onChange={(event) => setEventVisibility(event.currentTarget.checked)}
                color="green"
                aria-label="Event Attendance Visibility"
              />
            </Paper>

            <Paper
              style={{
                background: '#FAF6F2',
                padding: '16px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                transition: 'all 0.3s ease',
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = 'translateX(2px)'
                e.currentTarget.style.boxShadow = '0 1px 6px rgba(0,0,0,0.05)'
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = 'translateX(0)'
                e.currentTarget.style.boxShadow = 'none'
              }}
            >
              <Box>
                <Title order={4} style={{ fontSize: '15px', fontWeight: 600, marginBottom: '4px' }}>
                  Contact Information
                </Title>
                <Text style={{ color: '#8B8680', fontSize: '13px' }}>
                  Allow other vetted members to see your contact details
                </Text>
              </Box>
              <Switch
                checked={contactVisibility}
                onChange={(event) => setContactVisibility(event.currentTarget.checked)}
                color="green"
                aria-label="Contact Information"
              />
            </Paper>
          </Stack>
        </Paper>

        {/* Account Data Section */}
        <Paper
          style={{
            background: '#FFF8F0',
            padding: '16px',
            transition: 'all 0.3s ease',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.boxShadow = '0 1px 8px rgba(0,0,0,0.05)'
            e.currentTarget.style.transform = 'translateX(2px)'
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.boxShadow = 'none'
            e.currentTarget.style.transform = 'translateX(0)'
          }}
        >
          <Box mb="lg">
            <Title
              order={2}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Account Data
            </Title>
          </Box>

          <Group align="flex-start">
            <Box style={{ flex: 1, minWidth: '250px' }}>
              <Title
                order={4}
                style={{
                  fontFamily: "'Montserrat', sans-serif",
                  fontWeight: 600,
                  marginBottom: '4px',
                  color: '#2B2B2B',
                  fontSize: '15px',
                }}
              >
                Download Your Data
              </Title>
              <Text style={{ color: '#8B8680', marginBottom: '12px', fontSize: '13px' }}>
                Get a copy of your profile and event history
              </Text>
              <button onClick={handleDataDownload} className="btn btn-secondary">
                Request Data Download
              </button>
            </Box>
          </Group>
        </Paper>
      </Stack>
    </DashboardLayout>
  )
}
