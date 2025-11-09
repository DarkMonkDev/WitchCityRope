import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { RefundTicketModal } from '../RefundTicketModal';

describe('RefundTicketModal', () => {
  const mockOnClose = vi.fn();
  const mockOnConfirm = vi.fn();

  const defaultParticipant = {
    userId: 'user-456',
    name: 'John Smith',
    ticketAmount: 35.00,
    hasRsvp: true,
    volunteerShifts: []
  };

  const defaultProps = {
    opened: true,
    onClose: mockOnClose,
    participant: defaultParticipant,
    eventName: 'Rope Intensive Weekend',
    onConfirm: mockOnConfirm
  };

  const renderWithProviders = (props = {}) => {
    return render(
      <MantineProvider>
        <Notifications />
        <RefundTicketModal {...defaultProps} {...props} />
      </MantineProvider>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mockOnConfirm.mockResolvedValue(undefined);
  });

  it('renders modal with refund amount prominently', () => {
    renderWithProviders();

    expect(screen.getByTestId('refund-ticket-modal')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Refund Ticket?/i, level: 3 })).toBeInTheDocument();
    expect(screen.getByText('John Smith')).toBeInTheDocument();
    expect(screen.getByText(/"Rope Intensive Weekend"/i)).toBeInTheDocument();

    // Check for prominent refund amount display
    expect(screen.getByText(/Refund Amount/i)).toBeInTheDocument();
    expect(screen.getByText('$35.00')).toBeInTheDocument();
  });

  it('shows "Also remove RSVP" checkbox checked by default', () => {
    renderWithProviders();

    const alsoRemoveRsvpCheckbox = screen.getByTestId('also-remove-rsvp-checkbox');
    expect(alsoRemoveRsvpCheckbox).toBeChecked();
    expect(screen.getByText(/Also remove RSVP if present/i)).toBeInTheDocument();
  });

  it('updates warning when "Also remove RSVP" checkbox toggled', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const alsoRemoveRsvpCheckbox = screen.getByTestId('also-remove-rsvp-checkbox');

    // Initially checked - should show RSVP removal in impact list
    expect(screen.getByText(/Remove their RSVP/i)).toBeInTheDocument();

    // Uncheck the checkbox
    await user.click(alsoRemoveRsvpCheckbox);

    // RSVP removal should NOT be in impact list anymore
    await waitFor(() => {
      expect(screen.queryByText(/Remove their RSVP/i)).not.toBeInTheDocument();
    });

    // Refund should still be shown
    expect(screen.getByText(/Refund \$35.00 to John Smith/i)).toBeInTheDocument();

    // Check again
    await user.click(alsoRemoveRsvpCheckbox);

    // RSVP removal should be back in impact list
    await waitFor(() => {
      expect(screen.getByText(/Remove their RSVP/i)).toBeInTheDocument();
    });
  });

  it('shows volunteer shift warning when shifts exist', () => {
    const participantWithShifts = {
      ...defaultParticipant,
      volunteerShifts: ['Setup Crew', 'Cleanup Crew']
    };

    renderWithProviders({ participant: participantWithShifts });

    expect(screen.getByText(/This action will:/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove volunteer assignment: Setup Crew/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove volunteer assignment: Cleanup Crew/i)).toBeInTheDocument();
  });

  it('confirm button disabled until checkbox checked', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const submitButton = screen.getByTestId('refund-ticket-submit');
    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');

    // Initially disabled
    expect(submitButton).toBeDisabled();

    // Check the confirmation checkbox
    await user.click(confirmationCheckbox);

    // Now enabled
    await waitFor(() => {
      expect(submitButton).not.toBeDisabled();
    });

    // Uncheck the checkbox
    await user.click(confirmationCheckbox);

    // Disabled again
    await waitFor(() => {
      expect(submitButton).toBeDisabled();
    });
  });

  it('calls onConfirm with alsoRemoveRsvp value', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const submitButton = screen.getByTestId('refund-ticket-submit');
    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');
    const alsoRemoveRsvpCheckbox = screen.getByTestId('also-remove-rsvp-checkbox');

    // Check confirmation checkbox
    await user.click(confirmationCheckbox);

    // AlsoRemoveRsvp is checked by default, click submit
    await user.click(submitButton);

    // Verify onConfirm was called with alsoRemoveRsvp = true
    await waitFor(() => {
      expect(mockOnConfirm).toHaveBeenCalledTimes(1);
      expect(mockOnConfirm).toHaveBeenCalledWith(true);
    });

    // Reset and try with alsoRemoveRsvp = false
    vi.clearAllMocks();
    mockOnConfirm.mockResolvedValue(undefined);

    // Uncheck alsoRemoveRsvp
    await user.click(alsoRemoveRsvpCheckbox);

    // Re-check confirmation checkbox (it was reset when modal closed after first submit)
    await user.click(confirmationCheckbox);

    // Click submit again
    await user.click(submitButton);

    // Verify onConfirm was called with alsoRemoveRsvp = false
    await waitFor(() => {
      expect(mockOnConfirm).toHaveBeenCalledTimes(1);
      expect(mockOnConfirm).toHaveBeenCalledWith(false);
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

    const submitButton = screen.getByTestId('refund-ticket-submit');
    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');

    // Check confirmation checkbox and submit
    await user.click(confirmationCheckbox);
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
    const errorMessage = 'Payment processor unavailable';
    mockOnConfirm.mockRejectedValue({ detail: errorMessage });

    renderWithProviders();

    const submitButton = screen.getByTestId('refund-ticket-submit');
    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');

    // Check confirmation checkbox and submit
    await user.click(confirmationCheckbox);
    await user.click(submitButton);

    // Wait for error notification
    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument();
      expect(screen.getByText(errorMessage)).toBeInTheDocument();
    });

    // Modal should still be open
    expect(screen.getByTestId('refund-ticket-modal')).toBeInTheDocument();
  });

  it('closes modal on successful confirmation', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const submitButton = screen.getByTestId('refund-ticket-submit');
    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');

    // Check confirmation checkbox and submit
    await user.click(confirmationCheckbox);
    await user.click(submitButton);

    // Verify onClose was called
    await waitFor(() => {
      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });
  });

  it('closes modal when cancel clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    const cancelButton = screen.getByTestId('refund-ticket-cancel');

    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  it('resets state when modal closes', async () => {
    const user = userEvent.setup();
    const { rerender } = renderWithProviders();

    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');
    const alsoRemoveRsvpCheckbox = screen.getByTestId('also-remove-rsvp-checkbox');

    // Check both checkboxes
    await user.click(confirmationCheckbox);
    await user.click(alsoRemoveRsvpCheckbox); // Uncheck (it's checked by default)

    expect(confirmationCheckbox).toBeChecked();
    expect(alsoRemoveRsvpCheckbox).not.toBeChecked();

    // Close modal
    rerender(
      <MantineProvider>
        <Notifications />
        <RefundTicketModal {...defaultProps} opened={false} />
      </MantineProvider>
    );

    // Reopen modal
    rerender(
      <MantineProvider>
        <Notifications />
        <RefundTicketModal {...defaultProps} opened={true} />
      </MantineProvider>
    );

    // Checkboxes should be reset to defaults
    const newConfirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');
    const newAlsoRemoveRsvpCheckbox = screen.getByTestId('also-remove-rsvp-checkbox');

    expect(newConfirmationCheckbox).not.toBeChecked();
    expect(newAlsoRemoveRsvpCheckbox).toBeChecked(); // Default is checked
  });

  it('displays cannot undo warning', () => {
    renderWithProviders();

    expect(screen.getByText(/This action cannot be undone/i)).toBeInTheDocument();
  });

  it('hides "Also remove RSVP" checkbox when participant has no RSVP', () => {
    const participantWithoutRsvp = {
      ...defaultParticipant,
      hasRsvp: false
    };

    renderWithProviders({ participant: participantWithoutRsvp });

    expect(screen.queryByTestId('also-remove-rsvp-checkbox')).not.toBeInTheDocument();
    expect(screen.queryByText(/Also remove RSVP if present/i)).not.toBeInTheDocument();
  });

  it('shows refund amount with correct formatting', () => {
    const participantWithDecimal = {
      ...defaultParticipant,
      ticketAmount: 42.50
    };

    renderWithProviders({ participant: participantWithDecimal });

    expect(screen.getByText('$42.50')).toBeInTheDocument();
    expect(screen.getByText(/Refund \$42.50 to John Smith/i)).toBeInTheDocument();
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

    const submitButton = screen.getByTestId('refund-ticket-submit');
    const cancelButton = screen.getByTestId('refund-ticket-cancel');
    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');

    // Check confirmation checkbox and submit
    await user.click(confirmationCheckbox);
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

  it('shows all impact items correctly', () => {
    const participantWithEverything = {
      userId: 'user-789',
      name: 'Sarah Connor',
      ticketAmount: 50.00,
      hasRsvp: true,
      volunteerShifts: ['Registration Desk', 'Door Monitor']
    };

    renderWithProviders({ participant: participantWithEverything });

    // Should show all impact items
    expect(screen.getByText(/Refund \$50.00 to Sarah Connor/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove their RSVP/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove volunteer assignment: Registration Desk/i)).toBeInTheDocument();
    expect(screen.getByText(/Remove volunteer assignment: Door Monitor/i)).toBeInTheDocument();
  });
});
