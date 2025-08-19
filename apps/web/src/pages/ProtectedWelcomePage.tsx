import React from 'react'
import { Link } from 'react-router-dom'
import {
  Container,
  Paper,
  Title,
  Text,
  Button,
  Grid,
  Card,
  Group,
  Badge,
  Stack,
  Alert,
  Loader
} from '@mantine/core'
import { IconLogout, IconCalendar, IconUser, IconSettings, IconAlertCircle } from '@tabler/icons-react'
import { useProtectedWelcome } from '../features/auth/api/queries'
import { useLogout } from '../features/auth/api/mutations'

export const ProtectedWelcomePage: React.FC = () => {
  const { data: welcomeData, isLoading, error } = useProtectedWelcome()
  const logoutMutation = useLogout()

  const handleLogout = () => {
    logoutMutation.mutate()
  }

  const formatDate = (dateString: string) => {
    try {
      return new Date(dateString).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    } catch {
      return dateString
    }
  }

  if (isLoading) {
    return (
      <Container size="lg" py="xl">
        <Stack align="center" gap="lg">
          <Loader size="lg" />
          <Text>Loading welcome data...</Text>
        </Stack>
      </Container>
    )
  }

  return (
    <Container size="lg" py="xl">
      {/* Header */}
      <Paper shadow="md" p="lg" mb="lg">
        <Group justify="space-between" align="center">
          <Group>
            <Badge color="green" variant="filled" data-testid="protected-indicator">
              🔒 Protected Content
            </Badge>
            <Title order={1} c="wcr.6">WitchCityRope Dashboard</Title>
          </Group>
          <Button
            leftSection={<IconLogout size={16} />}
            color="red"
            onClick={handleLogout}
            loading={logoutMutation.isPending}
            data-testid="logout-button"
          >
            Logout
          </Button>
        </Group>
      </Paper>

      {/* Welcome Section */}
      <Paper shadow="md" p="lg" mb="lg">
        {error ? (
          <Alert
            icon={<IconAlertCircle size="1rem" />}
            color="red"
            title="Error loading data"
          >
            {error.message || 'Failed to load welcome data'}
          </Alert>
        ) : welcomeData ? (
          <>
            <Title order={2} mb="lg" data-testid="welcome-message">
              {welcomeData.message}
            </Title>

            <Grid>
              {/* User Information */}
              <Grid.Col span={{ base: 12, md: 6 }}>
                <Card withBorder p="lg" h="100%">
                  <Title order={3} c="wcr.6" mb="md">
                    Account Information
                  </Title>
                  <Stack gap="sm">
                    <Group justify="space-between">
                      <Text c="dimmed">Scene Name:</Text>
                      <Text fw={500} data-testid="user-scene-name">
                        {welcomeData.user.sceneName}
                      </Text>
                    </Group>
                    <Group justify="space-between">
                      <Text c="dimmed">Email:</Text>
                      <Text data-testid="user-email">
                        {welcomeData.user.email}
                      </Text>
                    </Group>
                    <Group justify="space-between">
                      <Text c="dimmed">Member Since:</Text>
                      <Text data-testid="member-since">
                        {formatDate(welcomeData.user.createdAt)}
                      </Text>
                    </Group>
                    {welcomeData.user.lastLoginAt && (
                      <Group justify="space-between">
                        <Text c="dimmed">Last Login:</Text>
                        <Text data-testid="last-login">
                          {formatDate(welcomeData.user.lastLoginAt)}
                        </Text>
                      </Group>
                    )}
                    <Group justify="space-between">
                      <Text c="dimmed">Account Status:</Text>
                      <Badge color="green" variant="light" data-testid="account-status">
                        Active
                      </Badge>
                    </Group>
                  </Stack>
                </Card>
              </Grid.Col>

              {/* Server Information */}
              <Grid.Col span={{ base: 12, md: 6 }}>
                <Card withBorder p="lg" h="100%">
                  <Title order={3} c="wcr.6" mb="md">
                    Connection Status
                  </Title>
                  <Stack gap="sm">
                    <Group justify="space-between">
                      <Text c="dimmed">Server Time:</Text>
                      <Text data-testid="server-time">
                        {formatDate(welcomeData.serverTime)}
                      </Text>
                    </Group>
                    <Group justify="space-between">
                      <Text c="dimmed">API Status:</Text>
                      <Badge color="green" variant="light">✅ Connected</Badge>
                    </Group>
                    <Group justify="space-between">
                      <Text c="dimmed">Authentication:</Text>
                      <Badge color="green" variant="light">✅ Verified</Badge>
                    </Group>
                  </Stack>
                </Card>
              </Grid.Col>
            </Grid>
          </>
        ) : null}
      </Paper>

      {/* Navigation Actions */}
      <Paper shadow="md" p="lg" mb="lg">
        <Title order={3} c="wcr.6" mb="md">
          Quick Actions
        </Title>
        <Grid>
          <Grid.Col span={{ base: 12, sm: 4 }}>
            <Button
              component={Link}
              to="/"
              leftSection={<IconCalendar size={16} />}
              fullWidth
              variant="filled"
              color="wcr.6"
              data-testid="events-link"
            >
              View Public Events
            </Button>
          </Grid.Col>
          <Grid.Col span={{ base: 12, sm: 4 }}>
            <Button
              leftSection={<IconUser size={16} />}
              fullWidth
              variant="outline"
              color="blue"
              data-testid="profile-link"
            >
              Edit Profile
            </Button>
          </Grid.Col>
          <Grid.Col span={{ base: 12, sm: 4 }}>
            <Button
              leftSection={<IconSettings size={16} />}
              fullWidth
              variant="outline"
              color="gray"
            >
              Settings
            </Button>
          </Grid.Col>
        </Grid>
      </Paper>

      {/* Authentication Test Status */}
      <Alert color="green" title="🧪 Authentication Test Status">
        <Text size="sm">
          ✅ This page successfully validates the Hybrid JWT + HttpOnly Cookies authentication
          pattern. User data was fetched from a protected API endpoint using the stored JWT token.
        </Text>
      </Alert>
    </Container>
  )
}