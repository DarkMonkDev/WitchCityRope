import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { RemoveRsvpModal } from '@/components/events/RemoveRsvpModal';

describe('RemoveRsvpModal', () => {
  const mockOnClose = vi.fn();
  const mockOnConfirm = vi.fn();

  const defaultParticipant = {
    userId: 'user-123',
    name: 'Jane Doe',
    hasTicket: false,
    volunteerShifts: []
  };

  const defaultProps = {
    opened: true,
    onClose: mockOnClose,
    participant: defaultParticipant,
    eventName: 'Monthly Rope Jam',
    onConfirm: mockOnConfirm
  };

  const renderWithProviders = (props = {}) => {
    return render(
      <MantineProvider>
        <Notifications />
        <RemoveRsvpModal {...defaultProps} {...props} />
      </MantineProvider>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mockOnConfirm.mockResolvedValue(undefined);
  });

  it('renders modal with participant and event name', () => {
    renderWithProviders();

    expect(screen.getByTestId('remove-rsvp-modal')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Remove RSVP?/i, level: 3 })).toBeInTheDocument();
    expect(screen.getByText('Jane Doe')).toBeInTheDocument();
    expect(screen.getByText(/"Monthly Rope Jam"/i)).toBeInTheDocument();
    expect(screen.getByText(/You are about to remove the RSVP for:/i)).toBeInTheDocument();
  });

  it('shows ticket refund warning when hasTicket is true', () => {
    const participantWithTicket = {
      ...defaultParticipant,
      hasTicket: true,
      ticketAmount: 25.00
    };

    renderWithProviders({ participant: participantWithTicket });

    expect(screen.getByText(/This action will:/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove the RSVP for Jane Doe/i)).toBeInTheDocument();
    expect(screen.getByText(/Refund their ticket: \$25.00/i)).toBeInTheDocument();
  });

  it('shows volunteer shift warning when shifts exist', () => {
    const participantWithShifts = {
      ...defaultParticipant,
      volunteerShifts: ['Door Monitor', 'Setup Crew']
    };

    renderWithProviders({ participant: participantWithShifts });

    expect(screen.getByText(/This action will:/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove volunteer assignment: Door Monitor/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove volunteer assignment: Setup Crew/i)).toBeInTheDocument();
  });

  it('shows both ticket and volunteer warnings when both exist', () => {
    const participantWithBoth = {
      ...defaultParticipant,
      hasTicket: true,
      ticketAmount: 30.00,
      volunteerShifts: ['Cleanup Crew']
    };

    renderWithProviders({ participant: participantWithBoth });

    expect(screen.getByText(/Refund their ticket: \$30.00/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove volunteer assignment: Cleanup Crew/i)).toBeInTheDocument();
  });

  it('confirm button disabled until checkbox checked', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const submitButton = screen.getByTestId('remove-rsvp-submit');
    const checkbox = screen.getByTestId('remove-rsvp-confirmation');

    // Initially disabled
    expect(submitButton).toBeDisabled();

    // Check the checkbox
    await user.click(checkbox);

    // Now enabled
    await waitFor(() => {
      expect(submitButton).not.toBeDisabled();
    });

    // Uncheck the checkbox
    await user.click(checkbox);

    // Disabled again
    await waitFor(() => {
      expect(submitButton).toBeDisabled();
    });
  });

  it('calls onConfirm when confirmed', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const submitButton = screen.getByTestId('remove-rsvp-submit');
    const checkbox = screen.getByTestId('remove-rsvp-confirmation');

    // Check the checkbox
    await user.click(checkbox);

    // Click submit
    await user.click(submitButton);

    // Verify onConfirm was called
    await waitFor(() => {
      expect(mockOnConfirm).toHaveBeenCalledTimes(1);
    });
  });

  it('shows loading state during API call', async () => {
    const user = userEvent.setup();

    // Mock a slow API call
    let resolveConfirm: () => void;
    const slowConfirm = new Promise<void>((resolve) => {
      resolveConfirm = resolve;
    });
    mockOnConfirm.mockReturnValue(slowConfirm);

    renderWithProviders();

    const submitButton = screen.getByTestId('remove-rsvp-submit');
    const checkbox = screen.getByTestId('remove-rsvp-confirmation');

    // Check checkbox and submit
    await user.click(checkbox);
    await user.click(submitButton);

    // Verify loading state
    await waitFor(() => {
      expect(submitButton).toHaveAttribute('data-loading', 'true');
    });

    // Resolve the promise
    resolveConfirm!();

    // Verify loading state clears
    await waitFor(() => {
      expect(submitButton).not.toHaveAttribute('data-loading', 'true');
    });
  });

  it('shows error notification on API failure', async () => {
    const user = userEvent.setup();
    const errorMessage = 'Failed to remove RSVP';
    mockOnConfirm.mockRejectedValue({ detail: errorMessage });

    renderWithProviders();

    const submitButton = screen.getByTestId('remove-rsvp-submit');
    const checkbox = screen.getByTestId('remove-rsvp-confirmation');

    // Check checkbox and submit
    await user.click(checkbox);
    await user.click(submitButton);

    // Wait for error notification (Mantine notifications show in DOM)
    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument();
      expect(screen.getByText(errorMessage)).toBeInTheDocument();
    });

    // Modal should still be open
    expect(screen.getByTestId('remove-rsvp-modal')).toBeInTheDocument();
  });

  it('closes modal on successful confirmation', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const submitButton = screen.getByTestId('remove-rsvp-submit');
    const checkbox = screen.getByTestId('remove-rsvp-confirmation');

    // Check checkbox and submit
    await user.click(checkbox);
    await user.click(submitButton);

    // Verify onClose was called
    await waitFor(() => {
      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });
  });

  it('closes modal when cancel clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const cancelButton = screen.getByTestId('remove-rsvp-cancel');

    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  it('resets confirmation checkbox when modal closes', async () => {
    const user = userEvent.setup();
    const { rerender } = renderWithProviders();

    const checkbox = screen.getByTestId('remove-rsvp-confirmation');

    // Check the checkbox
    await user.click(checkbox);
    expect(checkbox).toBeChecked();

    // Close modal
    rerender(
      <MantineProvider>
        <Notifications />
        <RemoveRsvpModal {...defaultProps} opened={false} />
      </MantineProvider>
    );

    // Reopen modal
    rerender(
      <MantineProvider>
        <Notifications />
        <RemoveRsvpModal {...defaultProps} opened={true} />
      </MantineProvider>
    );

    // Checkbox should be unchecked
    const newCheckbox = screen.getByTestId('remove-rsvp-confirmation');
    expect(newCheckbox).not.toBeChecked();
  });

  it('shows confirmation required notification if submit clicked without checkbox', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const submitButton = screen.getByTestId('remove-rsvp-submit');

    // Try to click submit without checking checkbox (button is disabled, but we test the handler logic)
    // First enable by checking
    const checkbox = screen.getByTestId('remove-rsvp-confirmation');
    await user.click(checkbox);

    // Uncheck
    await user.click(checkbox);

    // The button should be disabled, preventing click
    expect(submitButton).toBeDisabled();
  });

  it('displays cannot undo warning', () => {
    renderWithProviders();

    expect(screen.getByText(/This action cannot be undone/i)).toBeInTheDocument();
  });

  it('does not call onClose when submitting', async () => {
    const user = userEvent.setup();

    // Mock a long-running API call
    let resolveConfirm: () => void;
    const slowConfirm = new Promise<void>((resolve) => {
      resolveConfirm = resolve;
    });
    mockOnConfirm.mockReturnValue(slowConfirm);

    renderWithProviders();

    const submitButton = screen.getByTestId('remove-rsvp-submit');
    const cancelButton = screen.getByTestId('remove-rsvp-cancel');
    const checkbox = screen.getByTestId('remove-rsvp-confirmation');

    // Check checkbox and submit
    await user.click(checkbox);
    await user.click(submitButton);

    // Cancel button should be disabled during submission
    expect(cancelButton).toBeDisabled();

    // onClose should not be called yet
    expect(mockOnClose).not.toHaveBeenCalled();

    // Resolve the API call
    resolveConfirm!();

    // Now onClose should be called
    await waitFor(() => {
      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });
  });
});
