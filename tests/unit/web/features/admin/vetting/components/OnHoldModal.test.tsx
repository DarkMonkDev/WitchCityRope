import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { MantineProvider } from '@mantine/core';
import { OnHoldModal } from '../../../../../../apps/web/src/features/admin/vetting/components/OnHoldModal';
import { vettingAdminApi } from '../../../../../../apps/web/src/features/admin/vetting/services/vettingAdminApi';

// Mock the API service
vi.mock('../../../../../../apps/web/src/features/admin/vetting/services/vettingAdminApi', () => ({
  vettingAdminApi: {
    putApplicationOnHold: vi.fn(),
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

describe('OnHoldModal', () => {
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
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    expect(screen.getByText('Put Application On Hold')).toBeInTheDocument();
    expect(screen.getByText(/You are about to put.*John Doe.*application on hold/)).toBeInTheDocument();
    expect(screen.getByLabelText('Reason for putting on hold')).toBeInTheDocument();
  });

  it('shows validation error when submitting without reason', async () => {
    const { notifications } = await import('@mantine/notifications');
    
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const submitButton = screen.getByText('Put On Hold');
    await user.click(submitButton);

    expect(notifications.show).toHaveBeenCalledWith({
      title: 'Validation Error',
      message: 'Please provide a reason for putting this application on hold',
      color: 'yellow'
    });
    
    expect(mockVettingAdminApi.putApplicationOnHold).not.toHaveBeenCalled();
  });

  it('disables submit button when no reason provided', () => {
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const submitButton = screen.getByText('Put On Hold');
    expect(submitButton).toBeDisabled();
  });

  it('enables submit button when reason is provided', async () => {
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const reasonTextarea = screen.getByLabelText('Reason for putting on hold');
    const submitButton = screen.getByText('Put On Hold');
    
    await user.type(reasonTextarea, 'Need additional references');
    
    expect(submitButton).toBeEnabled();
  });

  it('submits form successfully with valid reason', async () => {
    const { notifications } = await import('@mantine/notifications');
    
    mockVettingAdminApi.putApplicationOnHold.mockResolvedValue({
      decisionId: 'decision-1',
      decisionType: 'OnHold',
      submittedAt: '2025-09-22T10:00:00Z',
      newApplicationStatus: 'OnHold',
      confirmationMessage: 'Application put on hold successfully',
      actionsTriggered: []
    });

    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const reasonTextarea = screen.getByLabelText('Reason for putting on hold');
    const submitButton = screen.getByText('Put On Hold');
    
    await user.type(reasonTextarea, 'Need additional references');
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockVettingAdminApi.putApplicationOnHold).toHaveBeenCalledWith(
        'app-1',
        'Need additional references'
      );
    });

    expect(notifications.show).toHaveBeenCalledWith({
      title: 'Application On Hold',
      message: "John Doe's application has been put on hold",
      color: 'blue'
    });
    
    expect(mockOnSuccess).toHaveBeenCalled();
    expect(mockOnClose).toHaveBeenCalled();
  });

  it('handles API error gracefully', async () => {
    const { notifications } = await import('@mantine/notifications');
    
    mockVettingAdminApi.putApplicationOnHold.mockRejectedValue({
      detail: 'Application not found',
      message: 'The specified application could not be found'
    });

    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const reasonTextarea = screen.getByLabelText('Reason for putting on hold');
    const submitButton = screen.getByText('Put On Hold');
    
    await user.type(reasonTextarea, 'Need additional references');
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
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const cancelButton = screen.getByText('Cancel');
    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
    expect(mockVettingAdminApi.putApplicationOnHold).not.toHaveBeenCalled();
  });

  it('clears form when modal is closed', async () => {
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const reasonTextarea = screen.getByLabelText('Reason for putting on hold');
    await user.type(reasonTextarea, 'Some reason');
    
    const cancelButton = screen.getByText('Cancel');
    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
    
    // Re-render with modal closed and reopened to test clearing
    render(<OnHoldModal {...defaultProps} opened={false} />, { wrapper: createWrapper() });
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });
    
    const newReasonTextarea = screen.getByLabelText('Reason for putting on hold');
    expect(newReasonTextarea).toHaveValue('');
  });

  it('shows loading state during submission', async () => {
    // Mock a delayed API response
    mockVettingAdminApi.putApplicationOnHold.mockImplementation(
      () => new Promise(resolve => setTimeout(resolve, 100))
    );

    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const reasonTextarea = screen.getByLabelText('Reason for putting on hold');
    const submitButton = screen.getByText('Put On Hold');
    
    await user.type(reasonTextarea, 'Need additional references');
    await user.click(submitButton);

    // Button should show loading state
    expect(submitButton).toBeDisabled();
    
    // Cancel button should also be disabled during loading
    const cancelButton = screen.getByText('Cancel');
    expect(cancelButton).toBeDisabled();
  });

  it('prevents closing modal during submission', async () => {
    mockVettingAdminApi.putApplicationOnHold.mockImplementation(
      () => new Promise(resolve => setTimeout(resolve, 100))
    );

    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const reasonTextarea = screen.getByLabelText('Reason for putting on hold');
    const submitButton = screen.getByText('Put On Hold');
    
    await user.type(reasonTextarea, 'Need additional references');
    await user.click(submitButton);

    // Try to close modal during loading
    const cancelButton = screen.getByText('Cancel');
    await user.click(cancelButton);

    // onClose should not be called during submission
    expect(mockOnClose).not.toHaveBeenCalled();
  });

  it('does not render when opened is false', () => {
    render(<OnHoldModal {...defaultProps} opened={false} />, { wrapper: createWrapper() });

    expect(screen.queryByText('Put Application On Hold')).not.toBeInTheDocument();
  });

  it('uses placeholder text correctly', () => {
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    expect(screen.getByPlaceholderText('Enter the reason for putting this application on hold...')).toBeInTheDocument();
  });

  it('validates reason is not just whitespace', async () => {
    const { notifications } = await import('@mantine/notifications');
    
    render(<OnHoldModal {...defaultProps} />, { wrapper: createWrapper() });

    const reasonTextarea = screen.getByLabelText('Reason for putting on hold');
    const submitButton = screen.getByText('Put On Hold');
    
    // Enter only whitespace
    await user.type(reasonTextarea, '   ');
    await user.click(submitButton);

    expect(notifications.show).toHaveBeenCalledWith({
      title: 'Validation Error',
      message: 'Please provide a reason for putting this application on hold',
      color: 'yellow'
    });
    
    expect(mockVettingAdminApi.putApplicationOnHold).not.toHaveBeenCalled();
  });
});
