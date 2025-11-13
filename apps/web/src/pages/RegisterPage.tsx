import React, { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useForm } from '@mantine/form'
import {
  Title,
  TextInput,
  PasswordInput,
  Text,
  Alert,
  Stack,
  Box,
  Flex,
  Checkbox,
  Group,
} from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useRegister } from '../features/auth/api/mutations'
import { useIsAuthenticated } from '../stores/authStore'

type RegisterFormData = {
  email: string
  sceneName: string
  password: string
}

export const RegisterPage: React.FC = () => {
  const location = useLocation()
  const isAuthenticated = useIsAuthenticated();
  const registerMutation = useRegister()
  const [termsAccepted, setTermsAccepted] = useState(false)

  // Mantine form with manual validation
  const form = useForm<RegisterFormData>({
    mode: 'uncontrolled',
    initialValues: {
      email: '',
      sceneName: '',
      password: '',
    },
    validate: {
      email: (value) => {
        if (!value) return 'Email is required'
        if (!/^\S+@\S+\.\S+$/.test(value)) return 'Invalid email format'
        return null
      },
      sceneName: (value) => {
        if (!value) return 'Scene name is required'
        if (value.length < 3) return 'Scene name must be at least 3 characters'
        if (value.length > 50) return 'Scene name must be less than 50 characters'
        if (!/^[a-zA-Z0-9\s]+$/.test(value)) return 'Scene name can only contain letters, numbers, and spaces'
        return null
      },
      password: (value) => {
        if (!value) return 'Password is required'
        if (value.length < 8) return 'Password must be at least 8 characters'
        if (!/[A-Z]/.test(value)) return 'Password must contain at least one uppercase letter'
        if (!/[a-z]/.test(value)) return 'Password must contain at least one lowercase letter'
        if (!/[0-9]/.test(value)) return 'Password must contain at least one number'
        if (!/[^A-Za-z0-9]/.test(value)) return 'Password must contain at least one special character'
        return null
      },
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
    registerMutation.mutate({
      ...values,
      termsOfServiceAccepted: termsAccepted,
    })
  }

  return (
    <Flex
      align="center"
      justify="center"
      data-testid="page-register"
      style={{
        minHeight: 'calc(100vh - 120px)',
        padding: 'var(--space-xl) var(--space-md)',
      }}
    >
      {/* Auth Card matching wireframe design */}
      <Box
        style={{
          background: 'var(--color-ivory)',
          borderRadius: '24px',
          boxShadow: '0 20px 25px rgba(0,0,0,0.1)',
          width: '100%',
          maxWidth: '480px',
          overflow: 'hidden',
          position: 'relative',
        }}
      >
        {/* Decorative header band with burgundy gradient */}
        <Box
          style={{
            background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
            padding: 'var(--space-2xl) var(--space-xl) var(--space-xl)',
            textAlign: 'center',
            position: 'relative',
            overflow: 'hidden',
          }}
        >
          {/* Subtle radial overlay effect */}
          <Box
            style={{
              position: 'absolute',
              top: '-50%',
              left: '-50%',
              width: '200%',
              height: '200%',
              background: 'radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%)',
              transform: 'rotate(45deg)',
              pointerEvents: 'none',
            }}
          />

          <Title
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '32px',
              fontWeight: 800,
              color: 'var(--color-ivory)',
              marginBottom: 'var(--space-xs)',
              position: 'relative',
            }}
          >
            Join Our Community
          </Title>
        </Box>

        {/* Form container */}
        <Box style={{ padding: 'var(--space-xl)' }}>
          <form onSubmit={form.onSubmit(handleSubmit)} data-testid="register-form">
            <Stack gap="md">
              {registerMutation.error && (
                <Alert icon={<IconAlertCircle />} color="red" data-testid="register-error">
                  {registerMutation.error.message || 'Registration failed. Please try again.'}
                </Alert>
              )}

              <Box>
                <Text
                  component="label"
                  style={{
                    display: 'block',
                    fontFamily: 'var(--font-heading)',
                    fontSize: '14px',
                    fontWeight: 600,
                    color: 'var(--color-smoke)',
                    marginBottom: 'var(--space-xs)',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                  }}
                >
                  Email
                </Text>
                <TextInput
                  placeholder="your@email.com"
                  required
                  data-testid="email-input"
                  key={form.key('email')}
                  {...form.getInputProps('email')}
                  styles={{
                    input: {
                      fontFamily: 'var(--font-body)',
                      fontSize: '16px',
                      border: '2px solid var(--color-taupe)',
                      borderRadius: '8px',
                      background: 'var(--color-ivory)',
                      color: 'var(--color-charcoal)',
                      padding: 'var(--space-sm) var(--space-md)',
                      '&:focus': {
                        borderColor: 'var(--color-burgundy)',
                        boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
                      },
                      '&::placeholder': {
                        color: 'var(--color-stone)',
                      },
                    },
                  }}
                />
              </Box>

              <Box>
                <Text
                  component="label"
                  style={{
                    display: 'block',
                    fontFamily: 'var(--font-heading)',
                    fontSize: '14px',
                    fontWeight: 600,
                    color: 'var(--color-smoke)',
                    marginBottom: 'var(--space-xs)',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                  }}
                >
                  Scene / Preferred Name
                </Text>
                <TextInput
                  placeholder="Your display name"
                  required
                  data-testid="scene-name-input"
                  key={form.key('sceneName')}
                  {...form.getInputProps('sceneName')}
                  styles={{
                    input: {
                      fontFamily: 'var(--font-body)',
                      fontSize: '16px',
                      border: '2px solid var(--color-taupe)',
                      borderRadius: '8px',
                      background: 'var(--color-ivory)',
                      color: 'var(--color-charcoal)',
                      padding: 'var(--space-sm) var(--space-md)',
                      '&:focus': {
                        borderColor: 'var(--color-burgundy)',
                        boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
                      },
                      '&::placeholder': {
                        color: 'var(--color-stone)',
                      },
                    },
                  }}
                />
                <Text
                  size="sm"
                  c="dimmed"
                  style={{
                    marginTop: 'var(--space-xs)',
                    fontSize: '12px',
                    color: 'var(--color-smoke)',
                  }}
                >
                  Your display name in the community (3-50 characters)
                </Text>
              </Box>

              <Box>
                <Text
                  component="label"
                  style={{
                    display: 'block',
                    fontFamily: 'var(--font-heading)',
                    fontSize: '14px',
                    fontWeight: 600,
                    color: 'var(--color-smoke)',
                    marginBottom: 'var(--space-xs)',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                  }}
                >
                  Password
                </Text>
                <PasswordInput
                  placeholder="Enter your password"
                  required
                  data-testid="password-input"
                  key={form.key('password')}
                  {...form.getInputProps('password')}
                  styles={{
                    input: {
                      fontFamily: 'var(--font-body)',
                      fontSize: '16px',
                      border: '2px solid var(--color-taupe)',
                      borderRadius: '8px',
                      background: 'var(--color-ivory)',
                      color: 'var(--color-charcoal)',
                      padding: 'var(--space-sm) var(--space-md)',
                      '&:focus': {
                        borderColor: 'var(--color-burgundy)',
                        boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
                      },
                      '&::placeholder': {
                        color: 'var(--color-stone)',
                      },
                    },
                  }}
                />
                <Text
                  size="sm"
                  c="dimmed"
                  style={{
                    marginTop: 'var(--space-xs)',
                    fontSize: '12px',
                    color: 'var(--color-smoke)',
                  }}
                >
                  8+ characters with uppercase, lowercase, number, and special character
                </Text>
              </Box>

              {/* Terms of Service Acceptance */}
              <Box mt="md">
                <Group gap="sm" align="flex-start">
                  <Checkbox
                    id="terms-checkbox"
                    checked={termsAccepted}
                    onChange={(event) => setTermsAccepted(event.currentTarget.checked)}
                    size="md"
                    color="var(--color-burgundy)"
                    data-testid="terms-checkbox"
                    mt={4}
                  />
                  <Text
                    component="label"
                    htmlFor="terms-checkbox"
                    size="sm"
                    style={{
                      cursor: 'pointer',
                      color: 'var(--color-charcoal)',
                      lineHeight: 1.5,
                    }}
                  >
                    I agree to the{' '}
                    <a
                      href="/terms-of-service"
                      target="_blank"
                      rel="noopener noreferrer"
                      style={{
                        color: 'var(--color-burgundy)',
                        textDecoration: 'underline',
                        fontWeight: 600,
                      }}
                      onClick={(e) => e.stopPropagation()}
                    >
                      Terms of Service
                    </a>
                  </Text>
                </Group>
              </Box>

              {/* Create Account button using Box to match login page styling */}
              <Box
                component="button"
                type="submit"
                disabled={registerMutation.isPending || !termsAccepted}
                data-testid="register-button"
                className="btn btn-primary"
                style={{
                  marginTop: 'var(--space-sm)',
                  width: '100%',
                  opacity: termsAccepted ? 1 : 0.5,
                  cursor: termsAccepted ? 'pointer' : 'not-allowed',
                }}
              >
                {registerMutation.isPending ? 'Creating Account...' : 'Create Account'}
              </Box>
            </Stack>
          </form>
        </Box>

        {/* Footer with login link */}
        <Box
          style={{
            textAlign: 'center',
            padding: 'var(--space-md)',
            background: 'var(--color-cream)',
            borderTop: '1px solid var(--color-taupe)',
          }}
        >
          <Stack gap="md" align="center">
            <Box
              component={Link}
              to="/login"
              data-testid="link-login"
              className="btn btn-secondary"
            >
              Already Have An Account? Sign In
            </Box>
          </Stack>
        </Box>
      </Box>
    </Flex>
  )
}