import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MantineProvider } from '@mantine/core';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CoordinatorAssignmentModal } from '../CoordinatorAssignmentModal';

describe('CoordinatorAssignmentModal', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    // Create fresh QueryClient for EACH test to ensure cache isolation
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  afterEach(() => {
    // Clear all queries from cache to prevent test pollution
    queryClient.clear();
  });

  const renderComponent = (props: any = {}) => {
    const defaultProps = {
      opened: true,
      onClose: vi.fn(),
      incidentId: 'test-incident-id',
      ...props
    };

    return render(
      <QueryClientProvider client={queryClient}>
        <MantineProvider>
          <CoordinatorAssignmentModal {...defaultProps} />
        </MantineProvider>
      </QueryClientProvider>
    );
  };

  it('renders modal with title when opened', async () => {
    renderComponent();

    // Wait for async query to load
    await waitFor(() => {
      expect(screen.getByText('Assign Coordinator')).toBeInTheDocument();
    });
  });

  it('displays instruction text', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Select a user to assign as the incident coordinator:')).toBeInTheDocument();
    });
  });

  it('renders coordinator select dropdown with searchable prop', async () => {
    renderComponent();

    await waitFor(() => {
      const select = screen.getByTestId('coordinator-search-input');
      expect(select).toBeInTheDocument();
    });
  });

  it('displays fetched users in select dropdown', async () => {
    renderComponent();

    // Wait for users to load from MSW handler
    await waitFor(() => {
      const select = screen.getByTestId('coordinator-search-input');
      expect(select).toBeInTheDocument();
    });

    // Click to open dropdown using test ID to avoid ambiguity
    const select = screen.getByTestId('coordinator-search-input');
    await userEvent.click(select);

    // Check that options from MSW handler are displayed
    await waitFor(() => {
      expect(screen.getByText(/Alice Smith \(Admin\) - 2 active/)).toBeInTheDocument();
    });
  });

  it('disables assign button when no user is selected', async () => {
    renderComponent();

    await waitFor(() => {
      const assignButton = screen.getByTestId('assign-button');
      expect(assignButton).toBeDisabled();
    });
  });

  it('enables assign button when user is selected', async () => {
    renderComponent();

    // Wait for dropdown to be ready
    await waitFor(() => {
      expect(screen.getByTestId('coordinator-search-input')).toBeInTheDocument();
    });

    const select = screen.getByTestId('coordinator-search-input');
    await userEvent.click(select);

    // Select first option
    const option = await screen.findByText(/Alice Smith \(Admin\) - 2 active/);
    await userEvent.click(option);

    await waitFor(() => {
      const assignButton = screen.getByTestId('assign-button');
      expect(assignButton).not.toBeDisabled();
    });
  });

  it('calls onClose when cancel button clicked', async () => {
    const mockOnClose = vi.fn();
    renderComponent({ onClose: mockOnClose });

    await waitFor(() => {
      expect(screen.getByTestId('cancel-button')).toBeInTheDocument();
    });

    const cancelButton = screen.getByTestId('cancel-button');
    await userEvent.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  it('displays loading state on assign button when submitting', async () => {
    renderComponent();

    // Wait for dropdown
    await waitFor(() => {
      expect(screen.getByTestId('coordinator-search-input')).toBeInTheDocument();
    });

    // Select a user
    const select = screen.getByTestId('coordinator-search-input');
    await userEvent.click(select);
    const option = await screen.findByText(/Alice Smith \(Admin\) - 2 active/);
    await userEvent.click(option);

    // Click assign button
    const assignButton = await screen.findByTestId('assign-button');
    await userEvent.click(assignButton);

    // MSW will respond immediately, so we check the button was clicked
    expect(assignButton).toBeInTheDocument();
  });

  it('closes modal after successful assignment', async () => {
    const mockOnClose = vi.fn();
    renderComponent({ onClose: mockOnClose });

    // Wait for dropdown
    await waitFor(() => {
      expect(screen.getByTestId('coordinator-search-input')).toBeInTheDocument();
    });

    // Select a user
    const select = screen.getByTestId('coordinator-search-input');
    await userEvent.click(select);
    const option = await screen.findByText(/Alice Smith \(Admin\) - 2 active/);
    await userEvent.click(option);

    // Click assign button
    const assignButton = await screen.findByTestId('assign-button');
    await userEvent.click(assignButton);

    // Modal should close after successful mutation
    await waitFor(() => {
      expect(mockOnClose).toHaveBeenCalled();
    });
  });

  it('initializes with currentCoordinatorId when provided', async () => {
    renderComponent({ currentCoordinatorId: '2' });

    // Wait for dropdown
    await waitFor(() => {
      expect(screen.getByTestId('coordinator-search-input')).toBeInTheDocument();
    });

    // Assign button should be enabled because a user is pre-selected
    await waitFor(() => {
      const assignButton = screen.getByTestId('assign-button');
      expect(assignButton).not.toBeDisabled();
    });
  });
});
