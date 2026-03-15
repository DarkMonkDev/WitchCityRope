import React from 'react'
import { Link } from 'react-router-dom'
import { useForm } from '@mantine/form'
import {
  Title,
  TextInput,
  Text,
  Alert,
  Stack,
  Box,
  Flex,
} from '@mantine/core'
import { IconAlertCircle, IconCircleCheck } from '@tabler/icons-react'
import { useForgotPassword } from '../features/auth/api/mutations'

type ForgotPasswordFormData = {
  email: string
}

/**
 * Forgot Password Page - Phase 3: Password Reset
 * Allows users to request a password reset link via email
 * Security: Always shows success message to prevent email enumeration
 */
export const ForgotPasswordPage: React.FC = () => {
  const forgotPasswordMutation = useForgotPassword()

  // Mantine form with validation
  const form = useForm<ForgotPasswordFormData>({
    mode: 'uncontrolled',
    initialValues: {
      email: '',
    },
    validate: {
      email: (value) => {
        if (!value || !value.trim()) return 'Email is required'
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return 'Invalid email format'
        return null
      },
    },
  })

  const handleSubmit = (values: ForgotPasswordFormData) => {
    forgotPasswordMutation.mutate({ email: values.email })
  }

  return (
    <Flex
      align="center"
      justify="center"
      data-testid="page-forgot-password"
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
            Create / Reset Password
          </Title>
          <Text
            style={{
              fontFamily: 'var(--font-body)',
              fontSize: '16px',
              color: 'var(--color-ivory)',
              opacity: 0.9,
              position: 'relative',
            }}
          >
            Enter your email to receive a reset link
          </Text>
        </Box>

        {/* Form container */}
        <Box style={{ padding: 'var(--space-xl)' }}>
          {forgotPasswordMutation.isSuccess ? (
            <Stack gap="lg">
              <Alert icon={<IconCircleCheck />} color="green" data-testid="success-message">
                If an account exists with this email, a password reset link has been sent. Please check your inbox.
              </Alert>
              <Text size="sm" c="dimmed" style={{ textAlign: 'center' }}>
                Didn't receive the email? Check your spam folder or try again in a few minutes.
              </Text>
              <Box
                component={Link}
                to="/login"
                data-testid="link-back-to-login"
                className="btn btn-secondary"
              >
                Back to Login
              </Box>
            </Stack>
          ) : (
            <form onSubmit={form.onSubmit(handleSubmit)} data-testid="forgot-password-form">
              <Stack gap="md">
                {forgotPasswordMutation.error && (
                  <Alert icon={<IconAlertCircle />} color="red" data-testid="error-message">
                    {forgotPasswordMutation.error.message || 'An error occurred. Please try again.'}
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
                    Email Address
                  </Text>
                  <TextInput
                    placeholder="email@example.com"
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

                {/* Send Reset Link button */}
                <Box
                  component="button"
                  type="submit"
                  disabled={forgotPasswordMutation.isPending}
                  data-testid="submit-button"
                  className="btn btn-primary"
                  style={{
                    marginTop: 'var(--space-sm)',
                    width: '100%',
                  }}
                >
                  {forgotPasswordMutation.isPending ? 'Sending...' : 'Send Reset Link'}
                </Box>

                {/* Back to Login link */}
                <Text size="sm" style={{ textAlign: 'center', marginTop: 'var(--space-xs)' }}>
                  Remember your password?{' '}
                  <Link
                    to="/login"
                    data-testid="link-back-to-login"
                    style={{
                      color: 'var(--color-burgundy)',
                      textDecoration: 'none',
                      fontWeight: 600,
                    }}
                  >
                    Back to Login
                  </Link>
                </Text>
              </Stack>
            </form>
          )}
        </Box>
      </Box>
    </Flex>
  )
}
