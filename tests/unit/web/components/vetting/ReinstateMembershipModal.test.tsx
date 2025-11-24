import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { Notifications, notifications } from '@mantine/notifications';
import { ReinstateMembershipModal } from '@/components/vetting/ReinstateMembershipModal';

// Mock the vettingHold API
vi.mock('@/services/vettingHold.api', () => ({
  vettingHoldService: {
    requestReinstatement: vi.fn(),
  },
}));

import { vettingHoldService } from '@/services/vettingHold.api';

describe('ReinstateMembershipModal', () => {
  const mockOnClose = vi.fn();
  const mockOnConfirm = vi.fn().mockResolvedValue(undefined);

  const defaultProps = {
    opened: true,
    onClose: mockOnClose,
    onConfirm: mockOnConfirm,
  };

  const renderWithProviders = (props = {}) => {
    return render(
      <MantineProvider>
        <Notifications />
        <ReinstateMembershipModal {...defaultProps} {...props} />
      </MantineProvider>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(vettingHoldService.requestReinstatement).mockResolvedValue({
      newStatus: 2,
      statusName: 'FinalReview',
      changedAt: '2025-11-09T12:00:00Z',
    });
  });

  it('renders modal when opened', () => {
    renderWithProviders();

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Request Membership Reinstatement/i })).toBeInTheDocument();
  });

  it('displays info message about review process', () => {
    renderWithProviders();

    expect(screen.getByText(/This will:/i)).toBeInTheDocument();
    expect(screen.getByText(/Submit your reinstatement request to administrators/i)).toBeInTheDocument();
    expect(screen.getByText(/Change your status to "Final Review"/i)).toBeInTheDocument();
    expect(screen.getByText(/Require administrator approval before reinstatement/i)).toBeInTheDocument();
  });

  it('textarea allows input up to 500 characters', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    expect(textarea).toBeInTheDocument();

    const testReason = 'A'.repeat(500);
    await user.type(textarea, testReason);

    expect(textarea).toHaveValue(testReason);
  });

  it('shows character count', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);

    await user.type(textarea, 'Ready to return');

    // Character count should show 15/500
    expect(screen.getByText(/15 \/ 500/)).toBeInTheDocument();
  });

  it('cancel button closes modal', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const cancelButton = screen.getByRole('button', { name: /Cancel/i });
    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  it('confirm button calls API with reason', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Ready to rejoin the community');

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(vettingHoldService.requestReinstatement).toHaveBeenCalledTimes(1);
      expect(vettingHoldService.requestReinstatement).toHaveBeenCalledWith('user-123', 'Ready to rejoin the community');
    });
  });

  it('shows loading state during API call', async () => {
    const user = userEvent.setup();
    vi.mocked(vettingHoldService.requestReinstatement).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    );

    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Test reason');

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    await user.click(confirmButton);

    // Button should be disabled and show loading state
    await waitFor(() => {
      expect(confirmButton).toBeDisabled();
    });
  });

  it('shows success notification on success', async () => {
    const user = userEvent.setup();
    const notificationsSpy = vi.spyOn(notifications, 'show');

    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Test reason');

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(notificationsSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Reinstatement Requested',
          message: expect.stringContaining('reinstatement request has been submitted'),
          color: 'green',
        })
      );
    });
  });

  it('calls onSuccess callback after successful submission', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Test reason');

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(mockOnConfirm).toHaveBeenCalledTimes(1);
    });
  });

  it('shows error notification on API error', async () => {
    const user = userEvent.setup();
    const notificationsSpy = vi.spyOn(notifications, 'show');
    vi.mocked(vettingHoldService.requestReinstatement).mockRejectedValue(new Error('API Error'));

    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Test reason');

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(notificationsSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Error',
          message: expect.stringContaining('Failed to request reinstatement'),
          color: 'red',
        })
      );
    });

    // onSuccess should NOT be called on error
    expect(mockOnConfirm).not.toHaveBeenCalled();
  });

  it('resets form when modal closes', async () => {
    const user = userEvent.setup();
    const { rerender } = renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Test reason');

    expect(textarea).toHaveValue('Test reason');

    // Close modal
    rerender(
      <MantineProvider>
        <Notifications />
        <ReinstateMembershipModal {...defaultProps} opened={false} />
      </MantineProvider>
    );

    // Reopen modal
    rerender(
      <MantineProvider>
        <Notifications />
        <ReinstateMembershipModal {...defaultProps} opened={true} />
      </MantineProvider>
    );

    // Textarea should be reset
    const newTextarea = screen.getByPlaceholderText(/Please explain why/i);
    expect(newTextarea).toHaveValue('');
  });

  it('disables confirm button when reason is empty', () => {
    renderWithProviders();

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    expect(confirmButton).toBeDisabled();
  });

  it('enables confirm button when reason is provided', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Test reason');

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    await waitFor(() => {
      expect(confirmButton).not.toBeDisabled();
    });
  });

  it('disables confirm button again when reason is cleared', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Test reason');

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    await waitFor(() => {
      expect(confirmButton).not.toBeDisabled();
    });

    // Clear the textarea
    await user.clear(textarea);

    await waitFor(() => {
      expect(confirmButton).toBeDisabled();
    });
  });

  it('does not call API when modal is closed', () => {
    renderWithProviders({ opened: false });

    expect(vettingHoldService.requestReinstatement).not.toHaveBeenCalled();
  });

  it('limits reason to 500 characters', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const textarea = screen.getByPlaceholderText(/Please explain why/i);

    // Try to enter more than 500 characters
    const longReason = 'A'.repeat(600);
    await user.type(textarea, longReason);

    // Should be truncated to 500
    expect(textarea).toHaveValue('A'.repeat(500));
  });
});
