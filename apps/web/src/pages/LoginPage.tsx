import React, { useMemo } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useForm } from '@mantine/form'
import {
  Title,
  TextInput,
  PasswordInput,
  Text,
  Alert,
  Group,
  Stack,
  Checkbox,
  Box,
  Flex,
} from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useLogin } from '../features/auth/api/mutations'

type LoginFormData = {
  emailOrSceneName: string // Changed from 'email' - accepts either email or scene name
  password: string
  rememberMe: boolean
  returnUrl?: string // Optional return URL to send to backend
}

export const LoginPage: React.FC = () => {
  const loginMutation = useLogin()
  const [searchParams] = useSearchParams()

  // Extract returnUrl from query parameters
  const returnUrl = useMemo(() => {
    const returnUrlParam = searchParams.get('returnUrl')
    if (returnUrlParam) {
      console.log('📍 Login page loaded with returnUrl:', returnUrlParam)
      return returnUrlParam
    }
    return undefined
  }, [searchParams])

  // Mantine form with manual validation
  const form = useForm<LoginFormData>({
    mode: 'uncontrolled',
    initialValues: {
      emailOrSceneName: '',
      password: '',
      rememberMe: true, // Default to checked
      returnUrl, // Include return URL in form values
    },
    validate: {
      emailOrSceneName: (value) => {
        if (!value || !value.trim()) return 'Email or Scene Name is required'
        return null
      },
      password: (value) => (!value ? 'Password is required' : null),
    },
  })

  // Navigation is handled by the useLogin mutation to avoid infinite loops

  const handleSubmit = (values: LoginFormData) => {
    // Pass returnUrl to API for backend validation
    loginMutation.mutate(values)
  }

  return (
    <Flex
      align="center"
      justify="center"
      data-testid="page-login"
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
            Welcome Back
          </Title>
        </Box>

        {/* Form container */}
        <Box style={{ padding: 'var(--space-xl)' }}>
          <form onSubmit={form.onSubmit(handleSubmit)} data-testid="login-form">
            <Stack gap="md">
              {loginMutation.error && (
                <Alert icon={<IconAlertCircle />} color="red" data-testid="login-error">
                  {loginMutation.error.message || 'Login failed. Please try again.'}
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
                  Email or Scene Name
                </Text>
                <TextInput
                  placeholder="Scene Name or email@example.com"
                  required
                  data-testid="email-or-scenename-input"
                  key={form.key('emailOrSceneName')}
                  {...form.getInputProps('emailOrSceneName')}
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

                {/* Remember Me and Reset Password on same line */}
                <Flex justify="space-between" align="center" style={{ marginTop: 'var(--space-xs)', paddingLeft: 'var(--space-xs)', paddingRight: 'var(--space-xs)' }}>
                  <Flex gap="xs" align="center">
                    <Checkbox
                      key={form.key('rememberMe')}
                      {...form.getInputProps('rememberMe', { type: 'checkbox' })}
                      style={{
                        input: {
                          width: '20px',
                          height: '20px',
                          accentColor: 'var(--color-burgundy)',
                        },
                      }}
                      data-testid="remember-me-checkbox"
                    />
                    <Text
                      style={{
                        fontSize: '14px',
                        color: 'var(--color-smoke)',
                        lineHeight: '20px',
                        marginTop: '1px',
                      }}
                    >
                      Remember Me
                    </Text>
                  </Flex>
                  <Link
                    to="/forgot-password"
                    data-testid="link-forgot-password"
                    style={{
                      color: 'var(--color-burgundy)',
                      textDecoration: 'none',
                      fontWeight: 600,
                      fontSize: '14px',
                      lineHeight: '20px',
                      marginTop: '1px',
                      transition: 'color 0.3s ease',
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.color = 'var(--color-burgundy-dark)'
                      e.currentTarget.style.textDecoration = 'underline'
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.color = 'var(--color-burgundy)'
                      e.currentTarget.style.textDecoration = 'none'
                    }}
                  >
                    Reset Password
                  </Link>
                </Flex>
              </Box>

              {/* Sign In button using Box to match nav menu styling - proper edge morphing animations */}
              <Box
                component="button"
                type="submit"
                disabled={loginMutation.isPending}
                data-testid="login-button"
                className="btn btn-primary"
                style={{
                  marginTop: 'var(--space-sm)',
                  width: '100%',
                }}
              >
                {loginMutation.isPending ? 'Signing In...' : 'Sign In'}
              </Box>
            </Stack>
          </form>
        </Box>

        {/* Footer with register links */}
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
              to="/register"
              data-testid="link-register"
              className="btn btn-secondary"
            >
              Create A New Account
            </Box>
          </Stack>

          {/* Test account info for development and staging only */}
          {(import.meta.env.DEV || import.meta.env.MODE === 'staging') && (
            <Box mt="lg">
              <Text
                fw={700}
                size="md"
                c="black"
                style={{ marginBottom: 'var(--space-xs)' }}
              >
                Test Accounts - All PWs Test123!
              </Text>
              <Stack gap="xs">
                <Text size="sm" c="black">
                  • RopeMaster (Admin)
                </Text>
                <Text size="sm" c="black">
                  • SafetyFirst (Teacher)
                </Text>
                <Text size="sm" c="black">
                  • RopeEnthusiast (Vetted Member)
                </Text>
                <Text size="sm" c="black">
                  • Learning (General Member)
                </Text>
              </Stack>
            </Box>
          )}
        </Box>
      </Box>
    </Flex>
  )
}
