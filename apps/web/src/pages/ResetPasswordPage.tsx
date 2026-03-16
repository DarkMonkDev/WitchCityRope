import React, { useState, useEffect } from 'react'
import { Link, useSearchParams, useNavigate } from 'react-router-dom'
import { useForm } from '@mantine/form'
import {
  Title,
  PasswordInput,
  Text,
  Alert,
  Stack,
  Box,
  Flex,
} from '@mantine/core'
import { IconAlertCircle, IconCircleCheck } from '@tabler/icons-react'
import { useResetPassword } from '../features/auth/api/mutations'

type ResetPasswordFormData = {
  newPassword: string
  confirmPassword: string
}

/**
 * Reset Password Page - Phase 3: Password Reset
 * Handles password reset link clicks from user's email
 * URL format: /reset-password?userId={guid}&token={token}
 * Pattern: Similar to VerifyEmailPage but with password form
 */
export const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [resetSuccessful, setResetSuccessful] = useState(false)

  // Extract reset parameters from URL
  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  // Mutation hook
  const resetPasswordMutation = useResetPassword()

  // Mantine form with validation
  const form = useForm<ResetPasswordFormData>({
    mode: 'uncontrolled',
    initialValues: {
      newPassword: '',
      confirmPassword: '',
    },
    validate: {
      newPassword: (value) => {
        if (!value || !value.trim()) return 'Password is required'
        if (value.length < 8) return 'Password must be at least 8 characters'
        if (!/[A-Z]/.test(value)) return 'Password must contain at least one uppercase letter'
        if (!/[a-z]/.test(value)) return 'Password must contain at least one lowercase letter'
        if (!/[0-9]/.test(value)) return 'Password must contain at least one number'
        if (!/[^A-Za-z0-9]/.test(value)) return 'Password must contain at least one special character'
        return null
      },
      confirmPassword: (value, values) => {
        if (!value || !value.trim()) return 'Please confirm your password'
        if (value !== values.newPassword) return 'Passwords do not match'
        return null
      },
    },
  })

  const handleSubmit = (values: ResetPasswordFormData) => {
    if (userId && token) {
      resetPasswordMutation.mutate({ userId, token, newPassword: values.newPassword })
    }
  }

  // Watch for successful reset and update UI
  useEffect(() => {
    if (resetPasswordMutation.isSuccess && !resetSuccessful) {
      setResetSuccessful(true)
      // Auto-redirect to login after 3 seconds
      setTimeout(() => {
        navigate('/login', { replace: true })
      }, 3000)
    }
  }, [resetPasswordMutation.isSuccess, resetSuccessful, navigate])

  // Missing parameters - show error
  if (!userId || !token) {
    return (
      <Flex
        align="center"
        justify="center"
        data-testid="page-reset-password"
        style={{
          minHeight: 'calc(100vh - 120px)',
          padding: 'var(--space-xl) var(--space-md)',
        }}
      >
        <Box
          style={{
            background: 'var(--color-ivory)',
            borderRadius: '24px',
            boxShadow: '0 20px 25px rgba(0,0,0,0.1)',
            width: '100%',
            maxWidth: '480px',
          }}
        >
          <Box style={{ padding: 'var(--space-xl)' }}>
            <Stack gap="lg" align="center">
              <IconAlertCircle size={64} color="var(--color-burgundy)" />
              <Title
                order={2}
                style={{
                  fontFamily: 'var(--font-heading)',
                  color: 'var(--color-burgundy)',
                  textAlign: 'center',
                }}
              >
                Invalid Reset Link
              </Title>
              <Text size="lg" style={{ textAlign: 'center', color: 'var(--color-smoke)' }}>
                The password reset link is invalid or incomplete. Please request a new password reset.
              </Text>

              <Box
                component={Link}
                to="/forgot-password"
                data-testid="link-forgot-password"
                className="btn btn-secondary"
              >
                Request New Reset Link
              </Box>
            </Stack>
          </Box>
        </Box>
      </Flex>
    )
  }

  // Success state
  if (resetSuccessful) {
    return (
      <Flex
        align="center"
        justify="center"
        data-testid="page-reset-password"
        style={{
          minHeight: 'calc(100vh - 120px)',
          padding: 'var(--space-xl) var(--space-md)',
        }}
      >
        <Box
          style={{
            background: 'var(--color-ivory)',
            borderRadius: '24px',
            boxShadow: '0 20px 25px rgba(0,0,0,0.1)',
            width: '100%',
            maxWidth: '480px',
            overflow: 'hidden',
          }}
        >
          {/* Success header with plum gradient */}
          <Box
            style={{
              background: 'linear-gradient(135deg, #9b4a75 0%, #614B79 100%)',
              padding: 'var(--space-2xl) var(--space-xl) var(--space-xl)',
              textAlign: 'center',
              position: 'relative',
              overflow: 'hidden',
            }}
          >
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
            <IconCircleCheck size={64} color="var(--color-ivory)" style={{ position: 'relative' }} />
            <Title
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '32px',
                fontWeight: 800,
                color: 'var(--color-ivory)',
                marginTop: 'var(--space-md)',
                position: 'relative',
              }}
            >
              Password Reset!
            </Title>
          </Box>

          {/* Content */}
          <Box style={{ padding: 'var(--space-xl)' }}>
            <Stack gap="lg">
              <Text size="lg" style={{ textAlign: 'center', color: 'var(--color-charcoal)' }}>
                Your password has been successfully reset. You can now log in with your new password.
              </Text>

              <Text size="sm" c="dimmed" style={{ textAlign: 'center' }}>
                Redirecting to login page...
              </Text>

              <Box
                component={Link}
                to="/login"
                data-testid="link-login"
                className="btn btn-primary"
              >
                Go to Login
              </Box>
            </Stack>
          </Box>
        </Box>
      </Flex>
    )
  }

  // Form state - show password reset form
  return (
    <Flex
      align="center"
      justify="center"
      data-testid="page-reset-password"
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
            Set New Password
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
            Choose a strong password for your account
          </Text>
        </Box>

        {/* Form container */}
        <Box style={{ padding: 'var(--space-xl)' }}>
          <form onSubmit={form.onSubmit(handleSubmit)} data-testid="reset-password-form">
            <Stack gap="md">
              {resetPasswordMutation.error && (
                <Alert icon={<IconAlertCircle />} color="red" data-testid="error-message">
                  {resetPasswordMutation.error.message || 'Password reset failed. Please try again.'}
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
                  New Password
                </Text>
                <PasswordInput
                  placeholder="Enter your new password"
                  required
                  data-testid="new-password-input"
                  key={form.key('newPassword')}
                  {...form.getInputProps('newPassword')}
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
                  Confirm Password
                </Text>
                <PasswordInput
                  placeholder="Re-enter your new password"
                  required
                  data-testid="confirm-password-input"
                  key={form.key('confirmPassword')}
                  {...form.getInputProps('confirmPassword')}
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

              {/* Reset Password button */}
              <Box
                component="button"
                type="submit"
                disabled={resetPasswordMutation.isPending}
                data-testid="submit-button"
                className="btn btn-primary"
                style={{
                  marginTop: 'var(--space-sm)',
                  width: '100%',
                }}
              >
                {resetPasswordMutation.isPending ? 'Resetting Password...' : 'Reset Password'}
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
        </Box>
      </Box>
    </Flex>
  )
}
