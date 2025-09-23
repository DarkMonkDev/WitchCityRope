import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { MantineProvider } from '@mantine/core';
import { SendReminderModal } from '../../../../../../apps/web/src/features/admin/vetting/components/SendReminderModal';
import { vettingAdminApi } from '../../../../../../apps/web/src/features/admin/vetting/services/vettingAdminApi';

// Mock the API service
vi.mock('../../../../../../apps/web/src/features/admin/vetting/services/vettingAdminApi', () => ({
  vettingAdminApi: {
    sendApplicationReminder: vi.fn(),
  },
}));

// Mock notifications
vi.mock('@mantine/notifications', () => ({
  notifications: {
    show: vi.fn(),
  },
}));

const mockVettingAdminApi = vi.mocked(vettingAdminApi);

const createWrapper = () => {
  return ({ children }: { children: React.ReactNode }) => (
    <MantineProvider>
      {children}
    </MantineProvider>
  );
};

describe('SendReminderModal', () => {
  const user = userEvent.setup();
  const mockOnClose = vi.fn();
  const mockOnSuccess = vi.fn();
  
  const defaultProps = {
    opened: true,
    onClose: mockOnClose,
    applicationId: 'app-1',
    applicantName: 'John Doe',
    onSuccess: mockOnSuccess,
  };
  
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders modal with correct title and content', () => {
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    expect(screen.getByText('Send Reminder')).toBeInTheDocument();
    expect(screen.getByText(/You are about to send a reminder for.*John Doe.*application/)).toBeInTheDocument();
    expect(screen.getByLabelText('Reminder message')).toBeInTheDocument();
  });

  it('pre-fills reminder message with default template', () => {
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const messageTextarea = screen.getByLabelText('Reminder message');
    expect(messageTextarea).toHaveValue(
      expect.stringContaining('Hi,\n\nThis is a friendly reminder regarding John Doe\'s application')
    );
    expect(messageTextarea).toHaveValue(
      expect.stringContaining('WitchCityRope Vetting Team')
    );
  });

  it('shows validation error when submitting without message', async () => {
    const { notifications } = await import('@mantine/notifications');
    
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    // Clear the pre-filled message
    const messageTextarea = screen.getByLabelText('Reminder message');
    await user.clear(messageTextarea);
    
    const submitButton = screen.getByText('Send Reminder');
    await user.click(submitButton);

    expect(notifications.show).toHaveBeenCalledWith({
      title: 'Validation Error',
      message: 'Please provide a reminder message',
      color: 'yellow'
    });
    
    expect(mockVettingAdminApi.sendApplicationReminder).not.toHaveBeenCalled();
  });

  it('disables submit button when no message provided', async () => {
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    // Clear the pre-filled message
    const messageTextarea = screen.getByLabelText('Reminder message');
    await user.clear(messageTextarea);
    
    const submitButton = screen.getByText('Send Reminder');
    expect(submitButton).toBeDisabled();
  });

  it('enables submit button when message is provided', () => {
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    // Should be enabled with pre-filled message
    const submitButton = screen.getByText('Send Reminder');
    expect(submitButton).toBeEnabled();
  });

  it('allows editing the pre-filled message', async () => {
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const messageTextarea = screen.getByLabelText('Reminder message');
    
    // Clear and type new message
    await user.clear(messageTextarea);
    await user.type(messageTextarea, 'Custom reminder message');
    
    expect(messageTextarea).toHaveValue('Custom reminder message');
    
    const submitButton = screen.getByText('Send Reminder');
    expect(submitButton).toBeEnabled();
  });

  it('submits form successfully with valid message', async () => {
    const { notifications } = await import('@mantine/notifications');
    
    mockVettingAdminApi.sendApplicationReminder.mockResolvedValue({
      success: true,
      message: 'Reminder sent successfully'
    });

    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const submitButton = screen.getByText('Send Reminder');
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockVettingAdminApi.sendApplicationReminder).toHaveBeenCalledWith(
        'app-1',
        expect.stringContaining('John Doe\'s application')
      );
    });

    expect(notifications.show).toHaveBeenCalledWith({
      title: 'Reminder Sent',
      message: "Reminder has been sent for John Doe's application",
      color: 'green'
    });
    
    expect(mockOnSuccess).toHaveBeenCalled();
    expect(mockOnClose).toHaveBeenCalled();
  });

  it('handles API error gracefully', async () => {
    const { notifications } = await import('@mantine/notifications');
    
    mockVettingAdminApi.sendApplicationReminder.mockRejectedValue({
      detail: 'Application not found',
      message: 'The specified application could not be found'
    });

    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const submitButton = screen.getByText('Send Reminder');
    await user.click(submitButton);

    await waitFor(() => {
      expect(notifications.show).toHaveBeenCalledWith({
        title: 'Error',
        message: 'Application not found',
        color: 'red'
      });
    });
    
    expect(mockOnSuccess).not.toHaveBeenCalled();
    expect(mockOnClose).not.toHaveBeenCalled();
  });

  it('handles cancel button click', async () => {
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const cancelButton = screen.getByText('Cancel');
    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
    expect(mockVettingAdminApi.sendApplicationReminder).not.toHaveBeenCalled();
  });

  it('clears form when modal is closed', async () => {
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const messageTextarea = screen.getByLabelText('Reminder message');
    await user.clear(messageTextarea);
    await user.type(messageTextarea, 'Custom message');
    
    const cancelButton = screen.getByText('Cancel');
    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  it('shows loading state during submission', async () => {
    // Mock a delayed API response
    mockVettingAdminApi.sendApplicationReminder.mockImplementation(
      () => new Promise(resolve => setTimeout(resolve, 100))
    );

    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const submitButton = screen.getByText('Send Reminder');
    await user.click(submitButton);

    // Button should show loading state
    expect(submitButton).toBeDisabled();
    
    // Cancel button should also be disabled during loading
    const cancelButton = screen.getByText('Cancel');
    expect(cancelButton).toBeDisabled();
  });

  it('prevents closing modal during submission', async () => {
    mockVettingAdminApi.sendApplicationReminder.mockImplementation(
      () => new Promise(resolve => setTimeout(resolve, 100))
    );

    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const submitButton = screen.getByText('Send Reminder');
    await user.click(submitButton);

    // Try to close modal during loading
    const cancelButton = screen.getByText('Cancel');
    await user.click(cancelButton);

    // onClose should not be called during submission
    expect(mockOnClose).not.toHaveBeenCalled();
  });

  it('does not render when opened is false', () => {
    render(<SendReminderModal {...defaultProps} opened={false} />, { wrapper: createWrapper() });

    expect(screen.queryByText('Send Reminder')).not.toBeInTheDocument();
  });

  it('uses placeholder text correctly', () => {
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    expect(screen.getByPlaceholderText('Enter the reminder message...')).toBeInTheDocument();
  });

  it('validates message is not just whitespace', async () => {
    const { notifications } = await import('@mantine/notifications');
    
    render(<SendReminderModal {...defaultProps} />, { wrapper: createWrapper() });

    const messageTextarea = screen.getByLabelText('Reminder message');
    
    // Clear and enter only whitespace
    await user.clear(messageTextarea);
    await user.type(messageTextarea, '   ');
    
    const submitButton = screen.getByText('Send Reminder');
    await user.click(submitButton);

    expect(notifications.show).toHaveBeenCalledWith({
      title: 'Validation Error',
      message: 'Please provide a reminder message',
      color: 'yellow'
    });
    
    expect(mockVettingAdminApi.sendApplicationReminder).not.toHaveBeenCalled();
  });

  it('pre-fills message only when modal is opened', () => {
    // Render closed modal first
    const { rerender } = render(
      <SendReminderModal {...defaultProps} opened={false} />, 
      { wrapper: createWrapper() }
    );

    // Then open it
    rerender(
      <SendReminderModal {...defaultProps} opened={true} />, 
      { wrapper: createWrapper() }
    );

    const messageTextarea = screen.getByLabelText('Reminder message');
    expect(messageTextarea).toHaveValue(
      expect.stringContaining('John Doe\'s application')
    );
  });

  it('handles different applicant names in template', () => {
    render(
      <SendReminderModal {...defaultProps} applicantName="Jane Smith" />, 
      { wrapper: createWrapper() }
    );

    const messageTextarea = screen.getByLabelText('Reminder message');
    expect(messageTextarea).toHaveValue(
      expect.stringContaining('Jane Smith\'s application')
    );
  });
});
