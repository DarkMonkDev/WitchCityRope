import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MantineProvider } from '@mantine/core';
import { CoordinatorAssignmentModal } from '../CoordinatorAssignmentModal';

const mockUsers = [
  { id: '1', sceneName: 'Admin User', realName: 'Alice Smith', role: 'Admin', activeIncidentCount: 2 },
  { id: '2', sceneName: 'Safety Coordinator', realName: 'Bob Johnson', role: 'Teacher', activeIncidentCount: 1 },
  { id: '3', sceneName: 'RopeTeacher', realName: 'Carol Martinez', role: 'Teacher', activeIncidentCount: 0 }
];

const renderComponent = (props: any = {}) => {
  const defaultProps = {
    opened: true,
    onClose: vi.fn(),
    onAssign: vi.fn(),
    incidentId: 'test-incident-id',
    allUsers: mockUsers,
    ...props
  };

  return render(
    <MantineProvider>
      <CoordinatorAssignmentModal {...defaultProps} />
    </MantineProvider>
  );
};

describe('CoordinatorAssignmentModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders modal with title when opened', () => {
    renderComponent();
    expect(screen.getByText('Assign Incident Coordinator')).toBeInTheDocument();
  });

  it('displays current coordinator when provided', () => {
    renderComponent({
      currentCoordinator: { id: '2', sceneName: 'Safety Coordinator' }
    });

    expect(screen.getByText(/Current Coordinator:/)).toBeInTheDocument();
    expect(screen.getByText('Safety Coordinator')).toBeInTheDocument();
  });

  it('does not display current coordinator alert when not assigned', () => {
    renderComponent({ currentCoordinator: undefined });

    expect(screen.queryByText(/Current Coordinator:/)).not.toBeInTheDocument();
  });

  it('renders user select dropdown with searchable prop', () => {
    renderComponent();

    const select = screen.getByTestId('coordinator-select');
    expect(select).toBeInTheDocument();
  });

  it('displays selected user details when user is chosen', async () => {
    const user = userEvent.setup();
    renderComponent();

    const select = screen.getByLabelText('Coordinator');
    await user.click(select);

    // Click the first option
    const option = await screen.findByText('Admin User (Alice Smith)');
    await user.click(option);

    await waitFor(() => {
      expect(screen.getByTestId('selected-user-details')).toBeInTheDocument();
      expect(screen.getByText('Admin User')).toBeInTheDocument();
      expect(screen.getByText('Real Name: Alice Smith')).toBeInTheDocument();
      expect(screen.getByText('Current Active Incidents: 2')).toBeInTheDocument();
      expect(screen.getByText('Role: Admin')).toBeInTheDocument();
    });
  });

  it('displays guidance text about any user being assignable', () => {
    renderComponent();

    expect(screen.getByText(/Any user can be assigned as coordinator/)).toBeInTheDocument();
  });

  it('displays email notification alert', () => {
    renderComponent();

    expect(screen.getByText(/They will receive email notification/)).toBeInTheDocument();
  });

  it('disables assign button when no user is selected', () => {
    renderComponent();

    const assignButton = screen.getByTestId('coordinator-assign-button');
    expect(assignButton).toBeDisabled();
  });

  it('enables assign button when user is selected', async () => {
    const user = userEvent.setup();
    renderComponent();

    const select = screen.getByLabelText('Coordinator');
    await user.click(select);

    const option = await screen.findByText('Admin User (Alice Smith)');
    await user.click(option);

    await waitFor(() => {
      const assignButton = screen.getByTestId('coordinator-assign-button');
      expect(assignButton).not.toBeDisabled();
    });
  });

  it('calls onAssign with selected user ID when assign button clicked', async () => {
    const user = userEvent.setup();
    const mockOnAssign = vi.fn().mockResolvedValue(undefined);
    renderComponent({ onAssign: mockOnAssign });

    const select = screen.getByLabelText('Coordinator');
    await user.click(select);

    const option = await screen.findByText('Admin User (Alice Smith)');
    await user.click(option);

    const assignButton = await screen.findByTestId('coordinator-assign-button');
    await user.click(assignButton);

    await waitFor(() => {
      expect(mockOnAssign).toHaveBeenCalledWith('1');
    });
  });

  it('calls onClose and resets form when cancel button clicked', async () => {
    const user = userEvent.setup();
    const mockOnClose = vi.fn();
    renderComponent({ onClose: mockOnClose });

    const cancelButton = screen.getByTestId('coordinator-cancel-button');
    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  it('displays loading state on assign button when submitting', async () => {
    const user = userEvent.setup();
    let resolveAssign: any;
    const mockOnAssign = vi.fn(() => new Promise((resolve) => { resolveAssign = resolve; }));
    renderComponent({ onAssign: mockOnAssign });

    const select = screen.getByLabelText('Coordinator');
    await user.click(select);

    const option = await screen.findByText('Admin User (Alice Smith)');
    await user.click(option);

    const assignButton = await screen.findByTestId('coordinator-assign-button');
    await user.click(assignButton);

    // Button should show loading state
    await waitFor(() => {
      expect(screen.getByTestId('coordinator-assign-button')).toBeDisabled();
    });

    // Resolve the promise
    resolveAssign();
  });

  it('closes modal after successful assignment', async () => {
    const user = userEvent.setup();
    const mockOnAssign = vi.fn().mockResolvedValue(undefined);
    const mockOnClose = vi.fn();
    renderComponent({ onAssign: mockOnAssign, onClose: mockOnClose });

    const select = screen.getByLabelText('Coordinator');
    await user.click(select);

    const option = await screen.findByText('Admin User (Alice Smith)');
    await user.click(option);

    const assignButton = await screen.findByTestId('coordinator-assign-button');
    await user.click(assignButton);

    await waitFor(() => {
      expect(mockOnClose).toHaveBeenCalled();
    });
  });

  it('handles assignment error gracefully', async () => {
    const user = userEvent.setup();
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    const mockOnAssign = vi.fn().mockRejectedValue(new Error('Assignment failed'));
    renderComponent({ onAssign: mockOnAssign });

    const select = screen.getByLabelText('Coordinator');
    await user.click(select);

    const option = await screen.findByText('Admin User (Alice Smith)');
    await user.click(option);

    const assignButton = await screen.findByTestId('coordinator-assign-button');
    await user.click(assignButton);

    await waitFor(() => {
      expect(consoleErrorSpy).toHaveBeenCalled();
    });

    consoleErrorSpy.mockRestore();
  });
});
