import React, { useEffect } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useForm } from '@mantine/form'
import { zodResolver } from 'mantine-form-zod-resolver'
import { z } from 'zod'
import {
  Container,
  Paper,
  Title,
  TextInput,
  PasswordInput,
  Button,
  Text,
  Alert,
  Group,
  Stack
} from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useLogin } from '../features/auth/api/mutations'
import { useAuth } from '../stores/authStore'

const loginSchema = z.object({
  email: z.string().email('Invalid email format'),
  password: z.string().min(1, 'Password is required'),
})

type LoginFormData = z.infer<typeof loginSchema>

export const LoginPage: React.FC = () => {
  const location = useLocation()
  const { isAuthenticated } = useAuth()
  const loginMutation = useLogin()

  // Mantine form with Zod validation
  const form = useForm<LoginFormData>({
    mode: 'uncontrolled',
    validate: zodResolver(loginSchema),
    initialValues: {
      email: '',
      password: '',
    },
  })

  // If already authenticated, the mutation will handle navigation
  useEffect(() => {
    if (isAuthenticated) {
      const urlParams = new URLSearchParams(location.search)
      const returnTo = urlParams.get('returnTo') || '/dashboard'
      window.location.href = returnTo
    }
  }, [isAuthenticated, location.search])

  const handleSubmit = (values: LoginFormData) => {
    loginMutation.mutate(values)
  }

  return (
    <Container size={420} my={40}>
      <Title ta="center" c="wcr.6" fw={900}>
        Welcome to WitchCityRope
      </Title>
      <Text c="dimmed" size="sm" ta="center" mt={5}>
        Don't have an account yet?{' '}
        <Link to="/register" style={{ color: 'var(--mantine-color-wcr-6)' }}>
          Create account
        </Link>
      </Text>

      <Paper withBorder shadow="md" p={30} mt={30} radius="md">
        <form onSubmit={form.onSubmit(handleSubmit)} data-testid="login-form">
          <Stack gap="md">
            {loginMutation.error && (
              <Alert
                icon={<IconAlertCircle size="1rem" />}
                color="red"
                data-testid="login-error"
              >
                {loginMutation.error.message || 'Login failed. Please try again.'}
              </Alert>
            )}

            <TextInput
              label="Email"
              placeholder="your@email.com"
              required
              data-testid="email-input"
              key={form.key('email')}
              {...form.getInputProps('email')}
            />

            <PasswordInput
              label="Password"
              placeholder="Your password"
              required
              data-testid="password-input"
              key={form.key('password')}
              {...form.getInputProps('password')}
            />

            <Button
              type="submit"
              fullWidth
              loading={loginMutation.isPending}
              color="wcr.6"
              data-testid="login-button"
            >
              {loginMutation.isPending ? 'Logging in...' : 'Login'}
            </Button>
          </Stack>
        </form>

        <Group justify="center" mt="lg">
          <Text size="sm" c="dimmed">
            Test Account: admin@witchcityrope.com / Test123!
          </Text>
        </Group>
      </Paper>
    </Container>
  )
}