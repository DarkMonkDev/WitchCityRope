import React, { useEffect, useState } from 'react'
import { useSearchParams, useNavigate, Link } from 'react-router-dom'
import {
  Container,
  Stack,
  Title,
  Text,
  Alert,
  Button,
  Paper,
  Flex,
  Box,
  LoadingOverlay,
} from '@mantine/core'
import { IconCircleCheck, IconAlertCircle, IconMail } from '@tabler/icons-react'
import { useVerifyEmail, useResendVerification } from '../features/auth/api/mutations'

/**
 * Email Verification Page
 * Handles email verification link clicks from user's inbox
 * Pattern: Similar to PaymentSuccessPage but for email verification
 *
 * URL format: /verify-email?userId={guid}&token={token}
 */
export const VerifyEmailPage: React.FC = () => {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [verificationAttempted, setVerificationAttempted] = useState(false)

  // Extract verification parameters from URL
  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  // Mutation hooks
  const verifyEmailMutation = useVerifyEmail()
  const resendMutation = useResendVerification()

  // Auto-submit verification on page load
  useEffect(() => {
    if (userId && token && !verificationAttempted) {
      setVerificationAttempted(true)
      verifyEmailMutation.mutate({ email: '', token })
    }
  }, [userId, token, verificationAttempted])

  // Handle resend verification email
  const handleResend = () => {
    const email = prompt('Please enter your email address to resend the verification link:')
    if (email) {
      resendMutation.mutate({ email })
    }
  }

  // Missing parameters - show error
  if (!userId || !token) {
    return (
      <Flex
        align="center"
        justify="center"
        data-testid="page-verify-email"
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
            maxWidth: '600px',
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
                Invalid Verification Link
              </Title>
              <Text size="lg" style={{ textAlign: 'center', color: 'var(--color-smoke)' }}>
                The verification link is invalid or incomplete. Please check your email for the complete verification link.
              </Text>

              <Button
                component={Link}
                to="/login"
                size="lg"
                color="blue"
                styles={{
                  root: {
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                  },
                }}
              >
                Go to Login
              </Button>
            </Stack>
          </Box>
        </Box>
      </Flex>
    )
  }

  // Loading state - verifying
  if (verifyEmailMutation.isPending) {
    return (
      <Flex
        align="center"
        justify="center"
        data-testid="page-verify-email"
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
            maxWidth: '600px',
            position: 'relative',
          }}
        >
          <LoadingOverlay visible={true} />
          <Box style={{ padding: 'var(--space-xl)' }}>
            <Stack gap="lg" align="center" py="xl">
              <Text size="lg" fw={600}>
                Verifying your email address...
              </Text>
              <Text size="sm" c="dimmed" style={{ textAlign: 'center' }}>
                Please wait while we confirm your email verification
              </Text>
            </Stack>
          </Box>
        </Box>
      </Flex>
    )
  }

  // Success state
  if (verifyEmailMutation.isSuccess) {
    return (
      <Flex
        align="center"
        justify="center"
        data-testid="page-verify-email"
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
            maxWidth: '600px',
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
              Email Verified!
            </Title>
          </Box>

          {/* Content */}
          <Box style={{ padding: 'var(--space-xl)' }}>
            <Stack gap="lg">
              <Text size="lg" style={{ textAlign: 'center', color: 'var(--color-charcoal)' }}>
                Your email address has been successfully verified. You can now log in to your account.
              </Text>

              <Button
                component={Link}
                to="/login"
                size="lg"
                color="blue"
                fullWidth
                styles={{
                  root: {
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                  },
                }}
                data-testid="goto-login-button"
              >
                Go to Login
              </Button>
            </Stack>
          </Box>
        </Box>
      </Flex>
    )
  }

  // Error state
  if (verifyEmailMutation.isError) {
    return (
      <Flex
        align="center"
        justify="center"
        data-testid="page-verify-email"
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
            maxWidth: '600px',
          }}
        >
          <Box style={{ padding: 'var(--space-xl)' }}>
            <Stack gap="lg">
              <Stack gap="md" align="center">
                <IconAlertCircle size={64} color="var(--color-burgundy)" />
                <Title
                  order={2}
                  style={{
                    fontFamily: 'var(--font-heading)',
                    color: 'var(--color-burgundy)',
                    textAlign: 'center',
                  }}
                >
                  Verification Failed
                </Title>
              </Stack>

              <Alert icon={<IconAlertCircle />} color="red">
                {verifyEmailMutation.error?.message || 'Email verification failed. Please try again.'}
              </Alert>

              {resendMutation.isSuccess ? (
                <Alert icon={<IconCircleCheck />} color="green">
                  Verification email sent! Please check your inbox.
                </Alert>
              ) : (
                <Button
                  onClick={handleResend}
                  variant="outline"
                  color="blue"
                  size="lg"
                  fullWidth
                  loading={resendMutation.isPending}
                  leftSection={<IconMail />}
                  styles={{
                    root: {
                      fontWeight: 600,
                      height: '44px',
                      paddingTop: '12px',
                      paddingBottom: '12px',
                      fontSize: '14px',
                      lineHeight: '1.2',
                    },
                  }}
                >
                  Resend Verification Email
                </Button>
              )}

              <Button
                component={Link}
                to="/login"
                variant="filled"
                color="blue"
                size="lg"
                fullWidth
                styles={{
                  root: {
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                  },
                }}
              >
                Go to Login
              </Button>
            </Stack>
          </Box>
        </Box>
      </Flex>
    )
  }

  return null
}
