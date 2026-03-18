import { useState } from 'react'
import { Title, Card, Group, Text, Modal, Stack, PasswordInput } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { WCRButton } from '../ui/WCRButton'
import { useAdminResetPassword } from '../../lib/api/hooks/useMemberDetails'

interface ResetPasswordSectionProps {
  memberId: string
  sceneName: string
}

export const ResetPasswordSection: React.FC<ResetPasswordSectionProps> = ({
  memberId,
  sceneName,
}) => {
  const [password, setPassword] = useState('')
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [showConfirmModal, setShowConfirmModal] = useState(false)
  const resetPasswordMutation = useAdminResetPassword()

  const validatePassword = (value: string): string | null => {
    if (!value || !value.trim()) return 'Password is required'
    if (value.length < 8) return 'Password must be at least 8 characters'
    if (!/[A-Z]/.test(value)) return 'Password must contain at least one uppercase letter'
    if (!/[a-z]/.test(value)) return 'Password must contain at least one lowercase letter'
    if (!/[0-9]/.test(value)) return 'Password must contain at least one number'
    if (!/[^A-Za-z0-9]/.test(value)) return 'Password must contain at least one special character'
    return null
  }

  const handleUpdateClick = () => {
    const error = validatePassword(password)
    if (error) {
      setPasswordError(error)
      return
    }
    setPasswordError(null)
    setShowConfirmModal(true)
  }

  const handleConfirmReset = async () => {
    try {
      await resetPasswordMutation.mutateAsync({
        userId: memberId,
        newPassword: password,
      })

      notifications.show({
        title: 'Success',
        message: `Password has been reset for ${sceneName}`,
        color: 'green',
      })

      setShowConfirmModal(false)
      setPassword('')
      setPasswordError(null)
    } catch (err: unknown) {
      setShowConfirmModal(false)
      const axiosError = err as { response?: { data?: { detail?: string } }; message?: string }
      notifications.show({
        title: 'Error',
        message: axiosError?.response?.data?.detail || axiosError?.message || 'Failed to reset password',
        color: 'red',
      })
    }
  }

  return (
    <>
      <div>
        <Title
          order={2}
          c="burgundy"
          mb="md"
          style={{
            borderBottom: '2px solid var(--mantine-color-burgundy-3)',
            paddingBottom: '8px',
          }}
        >
          Reset Password
        </Title>
        <Card withBorder p="lg" radius="md">
          <Group align="flex-start">
            <PasswordInput
              label="New Password"
              placeholder="Enter new password"
              value={password}
              onChange={(event) => {
                setPassword(event.currentTarget.value)
                if (passwordError) setPasswordError(null)
              }}
              error={passwordError}
              style={{ flex: 1 }}
            />
            <WCRButton
              variant="primary"
              onClick={handleUpdateClick}
              style={{ marginTop: '24px' }}
            >
              Update Password
            </WCRButton>
          </Group>
          <Text size="xs" c="dimmed" mt="xs">
            8+ characters with uppercase, lowercase, number, and special character
          </Text>
        </Card>
      </div>

      {/* Confirm Password Reset Modal */}
      <Modal
        opened={showConfirmModal}
        onClose={() => setShowConfirmModal(false)}
        title={<Text fw={600} size="lg">Reset Password</Text>}
        size="md"
      >
        <Stack gap="md">
          <Text size="sm" c="dimmed">
            Are you sure you want to reset the password for {sceneName}? They will need to use the
            new password to log in.
          </Text>

          <Group justify="flex-end" mt="md">
            <WCRButton
              variant="outline"
              onClick={() => setShowConfirmModal(false)}
            >
              Cancel
            </WCRButton>
            <WCRButton
              variant="primary"
              onClick={handleConfirmReset}
              loading={resetPasswordMutation.isPending}
            >
              Confirm
            </WCRButton>
          </Group>
        </Stack>
      </Modal>
    </>
  )
}
