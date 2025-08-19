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
  Stack
} from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useRegister } from '../features/auth/api/mutations'
import { useAuth } from '../stores/authStore'

const registerSchema = z.object({
  email: z.string().email('Invalid email format'),
  sceneName: z
    .string()
    .min(3, 'Scene name must be at least 3 characters')
    .max(50, 'Scene name must be less than 50 characters')
    .regex(/^[a-zA-Z0-9\s]+$/, 'Scene name can only contain letters, numbers, and spaces'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
    .regex(/[0-9]/, 'Password must contain at least one number')
    .regex(/[^A-Za-z0-9]/, 'Password must contain at least one special character'),
})

type RegisterFormData = z.infer<typeof registerSchema>

export const RegisterPage: React.FC = () => {
  const location = useLocation()
  const { isAuthenticated } = useAuth()
  const registerMutation = useRegister()

  // Mantine form with Zod validation
  const form = useForm<RegisterFormData>({
    mode: 'uncontrolled',
    validate: zodResolver(registerSchema),
    initialValues: {
      email: '',
      sceneName: '',
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

  const handleSubmit = (values: RegisterFormData) => {
    registerMutation.mutate(values)
  }

  return (
    <Container size={420} my={40}>
      <Title ta="center" c="wcr.6" fw={900}>
        Join WitchCityRope
      </Title>
      <Text c="dimmed" size="sm" ta="center" mt={5}>
        Already have an account?{' '}
        <Link to="/login" style={{ color: 'var(--mantine-color-wcr-6)' }}>
          Login here
        </Link>
      </Text>

      <Paper withBorder shadow="md" p={30} mt={30} radius="md">
        <form onSubmit={form.onSubmit(handleSubmit)} data-testid="register-form">
          <Stack gap="md">
            {registerMutation.error && (
              <Alert
                icon={<IconAlertCircle size="1rem" />}
                color="red"
                data-testid="register-error"
              >
                {registerMutation.error.message || 'Registration failed. Please try again.'}
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

            <TextInput
              label="Scene Name"
              placeholder="Your display name"
              description="Your display name in the community (3-50 characters)"
              required
              data-testid="scene-name-input"
              key={form.key('sceneName')}
              {...form.getInputProps('sceneName')}
            />

            <PasswordInput
              label="Password"
              placeholder="Your password"
              description="8+ characters with uppercase, lowercase, number, and special character"
              required
              data-testid="password-input"
              key={form.key('password')}
              {...form.getInputProps('password')}
            />

            <Button
              type="submit"
              fullWidth
              loading={registerMutation.isPending}
              color="wcr.6"
              data-testid="register-button"
            >
              {registerMutation.isPending ? 'Creating Account...' : 'Create Account'}
            </Button>
          </Stack>
        </form>
      </Paper>
    </Container>
  )
}