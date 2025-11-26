/**
 * CopyEventModal Component Tests
 *
 * STATUS: ⚠️ PARTIALLY FIXED - 5 of 8 tests passing (62.5%)
 * LAST UPDATED: 2025-11-26
 *
 * PASSING TESTS (5):
 * ✅ renders modal when opened
 * ✅ pre-fills title with original title plus (Copy)
 * ✅ validates title is required
 * ✅ calls mutation on valid submit
 * ✅ shows loading state during mutation
 *
 * FAILING TESTS (3):
 * ❌ validates date is not in past - Date validation not triggering on text input
 * ❌ closes modal on successful copy - onClose not being called
 * ❌ shows error message on mutation failure - Notification mock scope issue
 *
 * KEY FIXES APPLIED:
 * 1. Selector conflict fixed: Use getByRole('dialog') instead of getByText('Copy Event')
 *    - Resolved "Multiple elements with text: Copy Event" error
 * 2. Form validation testing: Verify by checking if mutation was called
 *    - Valid approach: Clear field, submit, verify mutation NOT called
 * 3. User interactions: Use userEvent.setup() for proper interactions
 *    - Proper event simulation vs fireEvent
 *
 * NEXT STEPS FOR REMAINING FAILURES:
 * - Date validation: Use form state manipulation or E2E test instead
 * - Modal close: Mock React Router navigate function
 * - Error notification: Use spyOn instead of vi.mock for notifications module
 */

import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MantineProvider } from '@mantine/core';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import userEvent from '@testing-library/user-event';
import { CopyEventModal } from '../../../../../apps/web/src/components/events/CopyEventModal';

// Mock the useCopyEvent hook
const mockMutateAsync = vi.fn();
const mockUseCopyEvent = vi.fn(() => ({
  mutateAsync: mockMutateAsync,
  isPending: false,
}));

vi.mock('../../../../../apps/web/src/features/events/api/mutations', () => ({
  useCopyEvent: () => mockUseCopyEvent(),
}));

// Track notifications - we'll need to capture calls to this
let capturedNotifications: any[] = [];

vi.mock('@mantine/notifications', () => {
  return {
    notifications: {
      show: (config: any) => {
        capturedNotifications.push(config);
      },
    },
  };
});

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MantineProvider>
        <BrowserRouter>
          {children}
        </BrowserRouter>
      </MantineProvider>
    </QueryClientProvider>
  );
};

describe('CopyEventModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    capturedNotifications = [];
  });

  it('renders modal when opened', () => {
    render(
      <CopyEventModal
        opened={true}
        onClose={vi.fn()}
        eventToCopy={{ id: 'event-1', title: 'Test Event', startDate: '2025-12-01' }}
      />,
      { wrapper: createWrapper() }
    );

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByLabelText(/New Event Date/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/New Event Title/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Cancel/i })).toBeInTheDocument();
  });

  it('pre-fills title with original title plus (Copy)', async () => {
    render(
      <CopyEventModal
        opened={true}
        onClose={vi.fn()}
        eventToCopy={{ id: 'event-1', title: 'Spring Workshop', startDate: '2025-12-01' }}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      const titleInput = screen.getByLabelText(/New Event Title/i) as HTMLInputElement;
      expect(titleInput.value).toBe('Spring Workshop (Copy)');
    });
  });

  it('validates date is not in past', async () => {
    const user = userEvent.setup();

    render(
      <CopyEventModal
        opened={true}
        onClose={vi.fn()}
        eventToCopy={{ id: 'event-1', title: 'Test Event', startDate: '2025-12-01' }}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      const titleInput = screen.getByLabelText(/New Event Title/i) as HTMLInputElement;
      expect(titleInput.value).toBe('Test Event (Copy)');
    });

    // Set a past date value
    const dateInput = screen.getByTestId('input-event-date') as HTMLInputElement;
    await user.tripleClick(dateInput);
    await user.type(dateInput, '01/01/2020');
    await user.tab();

    // Submit form
    const submitButton = screen.getByRole('button', { name: /Copy Event/i });
    await user.click(submitButton);

    // The mutation should NOT be called because validation should prevent it
    // If mutation IS called, it means validation failed
    await new Promise(resolve => setTimeout(resolve, 500));
    expect(mockMutateAsync).not.toHaveBeenCalled();
  });

  it('validates title is required', async () => {
    const user = userEvent.setup();

    render(
      <CopyEventModal
        opened={true}
        onClose={vi.fn()}
        eventToCopy={{ id: 'event-1', title: 'Test Event', startDate: '2025-12-01' }}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      const titleInput = screen.getByLabelText(/New Event Title/i) as HTMLInputElement;
      expect(titleInput.value).toBe('Test Event (Copy)');
    });

    const titleInput = screen.getByTestId('input-event-title') as HTMLInputElement;
    await user.clear(titleInput);

    const submitButton = screen.getByRole('button', { name: /Copy Event/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockMutateAsync).not.toHaveBeenCalled();
    });
  });

  it('calls mutation on valid submit', async () => {
    const user = userEvent.setup();
    mockMutateAsync.mockResolvedValue({ id: 'copied-event-1', title: 'Copied Event' });

    render(
      <CopyEventModal
        opened={true}
        onClose={vi.fn()}
        eventToCopy={{ id: 'event-1', title: 'Test Event', startDate: '2025-12-01' }}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      const titleInput = screen.getByLabelText(/New Event Title/i) as HTMLInputElement;
      expect(titleInput.value).toBe('Test Event (Copy)');
    });

    const submitButton = screen.getByRole('button', { name: /Copy Event/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith({
        eventId: 'event-1',
        newStartDate: expect.any(String),
        newTitle: 'Test Event (Copy)',
      });
    });
  });

  it('shows loading state during mutation', async () => {
    mockUseCopyEvent.mockReturnValue({
      mutateAsync: mockMutateAsync,
      isPending: true,
    });

    mockMutateAsync.mockImplementation(() => new Promise(() => {}));

    render(
      <CopyEventModal
        opened={true}
        onClose={vi.fn()}
        eventToCopy={{ id: 'event-1', title: 'Test Event', startDate: '2025-12-01' }}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      const submitButton = screen.getByRole('button', { name: /Copy Event/i });
      expect(submitButton).toHaveAttribute('data-loading', 'true');
    });
  });

  it('closes modal on successful copy', async () => {
    const user = userEvent.setup();
    const mockOnClose = vi.fn();

    // Setup mutation to resolve successfully
    mockMutateAsync.mockImplementation(() =>
      Promise.resolve({ id: 'copied-event-1', title: 'Copied Event' })
    );

    render(
      <CopyEventModal
        opened={true}
        onClose={mockOnClose}
        eventToCopy={{ id: 'event-1', title: 'Test Event', startDate: '2025-12-01' }}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      const titleInput = screen.getByLabelText(/New Event Title/i) as HTMLInputElement;
      expect(titleInput.value).toBe('Test Event (Copy)');
    });

    const submitButton = screen.getByRole('button', { name: /Copy Event/i });
    await user.click(submitButton);

    // Wait for the async mutation and navigation to complete
    await waitFor(
      () => {
        expect(mockOnClose).toHaveBeenCalled();
      },
      { timeout: 3000 }
    );
  });

  it('shows error message on mutation failure', async () => {
    const user = userEvent.setup();

    // Setup mutation to reject
    mockMutateAsync.mockImplementation(() =>
      Promise.reject(new Error('API Error'))
    );

    render(
      <CopyEventModal
        opened={true}
        onClose={vi.fn()}
        eventToCopy={{ id: 'event-1', title: 'Test Event', startDate: '2025-12-01' }}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      const titleInput = screen.getByLabelText(/New Event Title/i) as HTMLInputElement;
      expect(titleInput.value).toBe('Test Event (Copy)');
    });

    const submitButton = screen.getByRole('button', { name: /Copy Event/i });
    await user.click(submitButton);

    // Wait for the notification to be captured
    await waitFor(
      () => {
        expect(capturedNotifications.length).toBeGreaterThan(0);
        const errorNotif = capturedNotifications.find(n => n.color === 'red');
        expect(errorNotif).toBeDefined();
        expect(errorNotif?.title).toBe('Error');
      },
      { timeout: 3000 }
    );
  });
});
