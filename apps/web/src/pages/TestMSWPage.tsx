import React, { useState } from 'react'
import { Button, Container, Paper, Text, Title, Stack, Alert, Code } from '@mantine/core'
import { IconCheck, IconX, IconLoader } from '@tabler/icons-react'

/**
 * Test page to verify MSW mocking is working correctly
 * Tests various API endpoints to ensure mocking works
 */
export const TestMSWPage: React.FC = () => {
  const [results, setResults] = useState<Record<string, any>>({})
  const [isLoading, setIsLoading] = useState(false)

  const testEndpoint = async (name: string, url: string, method = 'GET') => {
    setIsLoading(true)
    try {
      const response = await fetch(url, {
        method,
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
      })
      
      const data = await response.json()
      setResults(prev => ({
        ...prev,
        [name]: {
          status: response.status,
          success: response.ok,
          data,
        }
      }))
    } catch (error) {
      setResults(prev => ({
        ...prev,
        [name]: {
          status: 'error',
          success: false,
          error: error instanceof Error ? error.message : 'Unknown error',
        }
      }))
    }
    setIsLoading(false)
  }

  const testLogin = async () => {
    setIsLoading(true)
    try {
      const response = await fetch('/api/Auth/login', {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: 'admin@witchcityrope.com',
          password: 'Test123!'
        })
      })
      
      const data = await response.json()
      setResults(prev => ({
        ...prev,
        login: {
          status: response.status,
          success: response.ok,
          data,
        }
      }))
    } catch (error) {
      setResults(prev => ({
        ...prev,
        login: {
          status: 'error',
          success: false,
          error: error instanceof Error ? error.message : 'Unknown error',
        }
      }))
    }
    setIsLoading(false)
  }

  const renderResult = (name: string, result: any) => {
    if (!result) return null

    const Icon = result.success ? IconCheck : IconX
    const color = result.success ? 'green' : 'red'

    return (
      <Alert
        key={name}
        icon={<Icon size="1rem" />}
        color={color}
        title={`${name} - Status: ${result.status}`}
      >
        <Code block>
          {JSON.stringify(result.data || result.error, null, 2)}
        </Code>
      </Alert>
    )
  }

  return (
    <Container size="lg" my={40}>
      <Title order={1} mb="lg">MSW Testing Page</Title>
      
      <Paper withBorder shadow="md" p="lg" mb="lg">
        <Stack gap="md">
          <Text size="lg" fw={600}>
            Test API Endpoints with MSW Mocking
          </Text>
          
          <Text c="dimmed">
            This page tests if MSW (Mock Service Worker) is correctly intercepting API calls.
            If MSW is enabled, you should see mock data responses instead of real API calls.
          </Text>

          <Alert color="blue">
            MSW Status: {import.meta.env.VITE_MSW_ENABLED === 'true' ? 'ENABLED' : 'DISABLED'}
          </Alert>

          <Stack gap="sm">
            <Button
              onClick={() => testEndpoint('profile', '/api/Protected/profile')}
              loading={isLoading}
              leftSection={isLoading ? <IconLoader size="1rem" /> : undefined}
            >
              Test Profile Endpoint
            </Button>

            <Button
              onClick={testLogin}
              loading={isLoading}
              leftSection={isLoading ? <IconLoader size="1rem" /> : undefined}
            >
              Test Login (admin@witchcityrope.com)
            </Button>

            <Button
              onClick={() => testEndpoint('events', '/api/events')}
              loading={isLoading}
              leftSection={isLoading ? <IconLoader size="1rem" /> : undefined}
            >
              Test Events List
            </Button>

            <Button
              onClick={() => testEndpoint('logout', '/api/Auth/logout', 'POST')}
              loading={isLoading}
              leftSection={isLoading ? <IconLoader size="1rem" /> : undefined}
            >
              Test Logout
            </Button>
          </Stack>
        </Stack>
      </Paper>

      {Object.keys(results).length > 0 && (
        <Paper withBorder shadow="md" p="lg">
          <Title order={3} mb="md">Test Results</Title>
          <Stack gap="md">
            {Object.entries(results).map(([name, result]) => renderResult(name, result))}
          </Stack>
        </Paper>
      )}
    </Container>
  )
}