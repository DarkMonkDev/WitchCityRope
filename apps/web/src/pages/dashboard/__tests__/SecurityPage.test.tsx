import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { MantineProvider } from '@mantine/core'
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest'
import { SecurityPage } from '../SecurityPage'

const createWrapper = () => {
  return ({ children }: { children: React.ReactNode }) => (
    <MantineProvider>
      <BrowserRouter>{children}</BrowserRouter>
    </MantineProvider>
  )
}

describe('SecurityPage', () => {
  beforeEach(() => {
    // Reset any console spies
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('should render page title and all sections', () => {
    render(<SecurityPage />, { wrapper: createWrapper() })

    expect(screen.getByText('Security Settings')).toBeInTheDocument()
    expect(screen.getByText('Change Password')).toBeInTheDocument()
    expect(screen.getByText('Two-Factor Authentication')).toBeInTheDocument()
    expect(screen.getByText('Privacy Settings')).toBeInTheDocument()
    expect(screen.getByText('Account Data')).toBeInTheDocument()
  })

  describe('Password Change Form', () => {
    it('should render all password form fields', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      expect(screen.getByLabelText('Current Password')).toBeInTheDocument()
      expect(screen.getByLabelText('New Password')).toBeInTheDocument()
      expect(screen.getByLabelText('Confirm New Password')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Update Password' })).toBeInTheDocument()
    })

    it('should display password requirements', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      expect(screen.getByText('Password Requirements:')).toBeInTheDocument()
      expect(screen.getByText('At least 8 characters long')).toBeInTheDocument()
      expect(screen.getByText('Include uppercase and lowercase letters')).toBeInTheDocument()
      expect(screen.getByText('Include at least one number')).toBeInTheDocument()
      expect(
        screen.getByText('Include at least one special character (!@#$%^&*)')
      ).toBeInTheDocument()
    })

    it('should validate current password is required', async () => {
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const submitButton = screen.getByRole('button', { name: 'Update Password' })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Current password is required')).toBeInTheDocument()
      })
    })

    it('should validate password minimum length', async () => {
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const currentPasswordInput = screen.getByLabelText('Current Password')
      const newPasswordInput = screen.getByLabelText('New Password')
      const submitButton = screen.getByRole('button', { name: 'Update Password' })

      await user.type(currentPasswordInput, 'current123!')
      await user.type(newPasswordInput, 'short')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Password must be at least 8 characters')).toBeInTheDocument()
      })
    })

    it('should validate password complexity requirements', async () => {
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const currentPasswordInput = screen.getByLabelText('Current Password')
      const newPasswordInput = screen.getByLabelText('New Password')
      const submitButton = screen.getByRole('button', { name: 'Update Password' })

      await user.type(currentPasswordInput, 'current123!')

      // Test missing uppercase and lowercase
      await user.clear(newPasswordInput)
      await user.type(newPasswordInput, 'alllowercase123!')
      await user.click(submitButton)

      await waitFor(() => {
        expect(
          screen.getByText('Password must contain uppercase and lowercase letters')
        ).toBeInTheDocument()
      })

      // Test missing number
      await user.clear(newPasswordInput)
      await user.type(newPasswordInput, 'NoNumbers!')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Password must contain at least one number')).toBeInTheDocument()
      })

      // Test missing special character
      await user.clear(newPasswordInput)
      await user.type(newPasswordInput, 'NoSpecial123')
      await user.click(submitButton)

      await waitFor(() => {
        expect(
          screen.getByText('Password must contain at least one special character')
        ).toBeInTheDocument()
      })
    })

    it('should validate password confirmation matches', async () => {
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const currentPasswordInput = screen.getByLabelText('Current Password')
      const newPasswordInput = screen.getByLabelText('New Password')
      const confirmPasswordInput = screen.getByLabelText('Confirm New Password')
      const submitButton = screen.getByRole('button', { name: 'Update Password' })

      await user.type(currentPasswordInput, 'current123!')
      await user.type(newPasswordInput, 'ValidPassword123!')
      await user.type(confirmPasswordInput, 'DifferentPassword123!')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Passwords do not match')).toBeInTheDocument()
      })
    })

    it('should submit form with valid data', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const currentPasswordInput = screen.getByLabelText('Current Password')
      const newPasswordInput = screen.getByLabelText('New Password')
      const confirmPasswordInput = screen.getByLabelText('Confirm New Password')
      const submitButton = screen.getByRole('button', { name: 'Update Password' })

      await user.type(currentPasswordInput, 'current123!')
      await user.type(newPasswordInput, 'ValidPassword123!')
      await user.type(confirmPasswordInput, 'ValidPassword123!')
      await user.click(submitButton)

      await waitFor(() => {
        expect(consoleSpy).toHaveBeenCalledWith('Password change submitted:', {
          currentPassword: 'current123!',
          newPassword: 'ValidPassword123!',
        })
      })

      // Form should reset after submission
      expect(currentPasswordInput).toHaveValue('')
      expect(newPasswordInput).toHaveValue('')
      expect(confirmPasswordInput).toHaveValue('')

      consoleSpy.mockRestore()
    })
  })

  describe('Two-Factor Authentication', () => {
    it('should show 2FA enabled state by default', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      expect(screen.getByText('Two-Factor Authentication Enabled')).toBeInTheDocument()
      expect(
        screen.getByText('Your account is protected with 2FA using your authenticator app')
      ).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Disable 2FA' })).toBeInTheDocument()
    })

    it('should disable 2FA when button is clicked', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const disableButton = screen.getByRole('button', { name: 'Disable 2FA' })
      await user.click(disableButton)

      await waitFor(() => {
        expect(consoleSpy).toHaveBeenCalledWith('Disabling 2FA...')
      })

      // Should switch to disabled state
      expect(
        screen.getByText('Two-Factor Authentication is disabled. Enable it for better security.')
      ).toBeInTheDocument()
      expect(screen.queryByText('Two-Factor Authentication Enabled')).not.toBeInTheDocument()

      consoleSpy.mockRestore()
    })
  })

  describe('Privacy Settings', () => {
    it('should render all privacy toggle switches with correct initial states', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      // Profile Visibility - should be enabled by default
      const profileToggle = screen.getByRole('switch', { name: /Profile Visibility/ })
      expect(profileToggle).toBeChecked()
      expect(
        screen.getByText('Make your profile visible to other community members')
      ).toBeInTheDocument()

      // Event Attendance Visibility - should be disabled by default
      const eventToggle = screen.getByRole('switch', { name: /Event Attendance Visibility/ })
      expect(eventToggle).not.toBeChecked()
      expect(
        screen.getByText("Show which events you're attending to other members")
      ).toBeInTheDocument()

      // Contact Information - should be disabled by default
      const contactToggle = screen.getByRole('switch', { name: /Contact Information/ })
      expect(contactToggle).not.toBeChecked()
      expect(
        screen.getByText('Allow other vetted members to see your contact details')
      ).toBeInTheDocument()
    })

    it('should toggle profile visibility setting', async () => {
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const profileToggle = screen.getByRole('switch', { name: /Profile Visibility/ })
      expect(profileToggle).toBeChecked()

      await user.click(profileToggle)
      expect(profileToggle).not.toBeChecked()

      await user.click(profileToggle)
      expect(profileToggle).toBeChecked()
    })

    it('should toggle event visibility setting', async () => {
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const eventToggle = screen.getByRole('switch', { name: /Event Attendance Visibility/ })
      expect(eventToggle).not.toBeChecked()

      await user.click(eventToggle)
      expect(eventToggle).toBeChecked()

      await user.click(eventToggle)
      expect(eventToggle).not.toBeChecked()
    })

    it('should toggle contact visibility setting', async () => {
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const contactToggle = screen.getByRole('switch', { name: /Contact Information/ })
      expect(contactToggle).not.toBeChecked()

      await user.click(contactToggle)
      expect(contactToggle).toBeChecked()

      await user.click(contactToggle)
      expect(contactToggle).not.toBeChecked()
    })

    it('should work independently for each privacy setting', async () => {
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const profileToggle = screen.getByRole('switch', { name: /Profile Visibility/ })
      const eventToggle = screen.getByRole('switch', { name: /Event Attendance Visibility/ })
      const contactToggle = screen.getByRole('switch', { name: /Contact Information/ })

      // Toggle each one individually
      await user.click(eventToggle)
      await user.click(contactToggle)

      // Check states are independent
      expect(profileToggle).toBeChecked() // Still original state
      expect(eventToggle).toBeChecked() // Changed
      expect(contactToggle).toBeChecked() // Changed

      await user.click(profileToggle)

      expect(profileToggle).not.toBeChecked() // Changed
      expect(eventToggle).toBeChecked() // Unchanged
      expect(contactToggle).toBeChecked() // Unchanged
    })
  })

  describe('Account Data', () => {
    it('should render data download section', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      expect(screen.getByText('Download Your Data')).toBeInTheDocument()
      expect(screen.getByText('Get a copy of your profile and event history')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Request Data Download' })).toBeInTheDocument()
    })

    it('should handle data download request', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
      const user = userEvent.setup()
      render(<SecurityPage />, { wrapper: createWrapper() })

      const downloadButton = screen.getByRole('button', { name: 'Request Data Download' })
      await user.click(downloadButton)

      await waitFor(() => {
        expect(consoleSpy).toHaveBeenCalledWith('Requesting data download...')
      })

      consoleSpy.mockRestore()
    })
  })

  describe('Visual Design', () => {
    it('should apply hover effects to paper sections', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      // Get one of the paper sections (password change section)
      const passwordSection = screen
        .getByText('Change Password')
        .closest('div[style*="background: #FFF8F0"]')
      expect(passwordSection).toBeInTheDocument()

      // Fire mouse events to test hover behavior
      if (passwordSection) {
        fireEvent.mouseEnter(passwordSection)
        fireEvent.mouseLeave(passwordSection)
      }
      // Note: Testing actual style changes would require jsdom style calculation
      // This test ensures the event handlers don't throw errors
    })

    it('should apply hover effects to privacy setting cards', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      // Get one of the privacy setting cards
      const profileCard = screen
        .getByText('Profile Visibility')
        .closest('div[style*="background: #FAF6F2"]')
      expect(profileCard).toBeInTheDocument()

      // Fire mouse events to test hover behavior
      if (profileCard) {
        fireEvent.mouseEnter(profileCard)
        fireEvent.mouseLeave(profileCard)
      }
      // Note: Testing actual style changes would require jsdom style calculation
      // This test ensures the event handlers don't throw errors
    })
  })

  describe('Form Accessibility', () => {
    it('should have proper labels for all form inputs', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      // All password inputs should have labels
      expect(screen.getByLabelText('Current Password')).toBeInTheDocument()
      expect(screen.getByLabelText('New Password')).toBeInTheDocument()
      expect(screen.getByLabelText('Confirm New Password')).toBeInTheDocument()

      // All toggle switches should have accessible names
      expect(screen.getByRole('switch', { name: /Profile Visibility/ })).toBeInTheDocument()
      expect(
        screen.getByRole('switch', { name: /Event Attendance Visibility/ })
      ).toBeInTheDocument()
      expect(screen.getByRole('switch', { name: /Contact Information/ })).toBeInTheDocument()
    })

    it('should have required attributes on password inputs', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      const currentPasswordInput = screen.getByLabelText('Current Password')
      const newPasswordInput = screen.getByLabelText('New Password')
      const confirmPasswordInput = screen.getByLabelText('Confirm New Password')

      expect(currentPasswordInput).toBeRequired()
      expect(newPasswordInput).toBeRequired()
      expect(confirmPasswordInput).toBeRequired()
    })

    it('should have correct input types for security', () => {
      render(<SecurityPage />, { wrapper: createWrapper() })

      const currentPasswordInput = screen.getByLabelText('Current Password')
      const newPasswordInput = screen.getByLabelText('New Password')
      const confirmPasswordInput = screen.getByLabelText('Confirm New Password')

      expect(currentPasswordInput).toHaveAttribute('type', 'password')
      expect(newPasswordInput).toHaveAttribute('type', 'password')
      expect(confirmPasswordInput).toHaveAttribute('type', 'password')
    })
  })
})
