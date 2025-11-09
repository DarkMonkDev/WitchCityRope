import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { EventForm } from '../EventForm';
import type { EventParticipationDto } from '../../../lib/api/hooks/useEventParticipations';

// Mock the hooks
vi.mock('../../../lib/api/hooks/useEventParticipations', () => ({
  useEventParticipations: vi.fn()
}));

vi.mock('../../../lib/api/hooks/useTeachers', () => ({
  useTeachers: vi.fn(() => ({ data: [], isLoading: false })),
  formatTeachersForMultiSelect: vi.fn(() => [])
}));

vi.mock('../../../lib/api/hooks/useEvents', () => ({
  useUpdateEvent: vi.fn(() => ({
    mutate: vi.fn(),
    isPending: false
  }))
}));

import { useEventParticipations } from '../../../lib/api/hooks/useEventParticipations';

describe('EventForm - Admin Actions', () => {
  let queryClient: QueryClient;
  const mockOnSubmit = vi.fn();

  const mockParticipations: EventParticipationDto[] = [
    {
      id: 'participation-1',
      userId: 'user-1',
      userSceneName: 'Alice Wonderland',
      attendanceType: 'RSVP',
      status: 'Active',
      metadata: JSON.stringify({
        hasTicket: true,
        ticketAmount: 25.00,
        volunteerShifts: ['Door Monitor']
      }),
      createdAt: '2025-11-01T10:00:00Z',
      updatedAt: '2025-11-01T10:00:00Z'
    },
    {
      id: 'participation-2',
      userId: 'user-2',
      userSceneName: 'Bob Builder',
      attendanceType: 'Ticket',
      status: 'Active',
      metadata: JSON.stringify({
        purchaseAmount: 35.00,
        ticketTypes: [{ name: 'Standard Ticket' }]
      }),
      createdAt: '2025-11-02T10:00:00Z',
      updatedAt: '2025-11-02T10:00:00Z'
    },
    {
      id: 'participation-3',
      userId: 'user-3',
      userSceneName: 'Charlie Chaplin',
      attendanceType: 'RSVP',
      status: 'Active',
      metadata: null,
      createdAt: '2025-11-03T10:00:00Z',
      updatedAt: '2025-11-03T10:00:00Z'
    }
  ];

  const defaultFormValues = {
    title: 'Test Event',
    eventType: 'Social' as const,
    startTime: new Date('2025-12-01T19:00:00Z'),
    endTime: new Date('2025-12-01T22:00:00Z'),
    capacity: 50,
    isPublic: true,
    description: 'Test event description',
    location: 'Test Venue',
    registrationDeadline: null,
    requiresVetting: false,
    organizerIds: [],
    teacherIds: []
  };

  const renderWithProviders = (props = {}) => {
    return render(
      <QueryClientProvider client={queryClient}>
        <MantineProvider>
          <Notifications />
          <EventForm
            initialValues={defaultFormValues}
            onSubmit={mockOnSubmit}
            eventId="event-123"
            {...props}
          />
        </MantineProvider>
      </QueryClientProvider>
    );
  };

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
    });

    vi.clearAllMocks();

    // Mock successful participations fetch
    (useEventParticipations as any).mockReturnValue({
      data: mockParticipations,
      isLoading: false
    });
  });

  afterEach(() => {
    queryClient.clear();
  });

  it('shows "Remove" link in RSVP table for active participations', async () => {
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await userEvent.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Alice Wonderland')).toBeInTheDocument();
    });

    // Find the RSVP table row for Alice
    const aliceRow = screen.getByText('Alice Wonderland').closest('tr');
    expect(aliceRow).toBeInTheDocument();

    // Should have a Remove link
    const removeLink = within(aliceRow!).getByTestId('remove-rsvp-participation-1');
    expect(removeLink).toBeInTheDocument();
    expect(removeLink).toHaveTextContent(/remove/i);
  });

  it('shows "Refund" link in Tickets table for active participations', async () => {
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await userEvent.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Bob Builder')).toBeInTheDocument();
    });

    // Find the Tickets table row for Bob
    const bobRow = screen.getByText('Bob Builder').closest('tr');
    expect(bobRow).toBeInTheDocument();

    // Should have a Refund link
    const refundLink = within(bobRow!).getByTestId('refund-ticket-participation-2');
    expect(refundLink).toBeInTheDocument();
    expect(refundLink).toHaveTextContent(/refund/i);
  });

  it('opens RemoveRsvpModal when Remove clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Alice Wonderland')).toBeInTheDocument();
    });

    // Click Remove link
    const removeLink = screen.getByTestId('remove-rsvp-participation-1');
    await user.click(removeLink);

    // RemoveRsvpModal should be visible
    await waitFor(() => {
      expect(screen.getByTestId('remove-rsvp-modal')).toBeInTheDocument();
      expect(screen.getByText(/Remove RSVP?/i)).toBeInTheDocument();
    });
  });

  it('opens RefundTicketModal when Refund clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Bob Builder')).toBeInTheDocument();
    });

    // Click Refund link
    const refundLink = screen.getByTestId('refund-ticket-participation-2');
    await user.click(refundLink);

    // RefundTicketModal should be visible
    await waitFor(() => {
      expect(screen.getByTestId('refund-ticket-modal')).toBeInTheDocument();
      expect(screen.getByText(/Refund Ticket?/i)).toBeInTheDocument();
    });
  });

  it('passes correct participant data to RemoveRsvpModal', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Alice Wonderland')).toBeInTheDocument();
    });

    // Click Remove link
    const removeLink = screen.getByTestId('remove-rsvp-participation-1');
    await user.click(removeLink);

    // Modal should show participant name
    await waitFor(() => {
      expect(screen.getByTestId('remove-rsvp-modal')).toBeInTheDocument();
      expect(screen.getByText('Alice Wonderland')).toBeInTheDocument();
    });
  });

  it('passes correct participant data to RefundTicketModal', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Bob Builder')).toBeInTheDocument();
    });

    // Click Refund link
    const refundLink = screen.getByTestId('refund-ticket-participation-2');
    await user.click(refundLink);

    // Modal should show participant name and amount
    await waitFor(() => {
      expect(screen.getByTestId('refund-ticket-modal')).toBeInTheDocument();
      expect(screen.getByText('Bob Builder')).toBeInTheDocument();
      expect(screen.getByText('$35.00')).toBeInTheDocument();
    });
  });

  it('shows success notification after RSVP removal', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Charlie Chaplin')).toBeInTheDocument();
    });

    // Click Remove link
    const removeLink = screen.getByTestId('remove-rsvp-participation-3');
    await user.click(removeLink);

    // Wait for modal
    await waitFor(() => {
      expect(screen.getByTestId('remove-rsvp-modal')).toBeInTheDocument();
    });

    // Check confirmation checkbox
    const confirmationCheckbox = screen.getByTestId('remove-rsvp-confirmation');
    await user.click(confirmationCheckbox);

    // Click submit
    const submitButton = screen.getByTestId('remove-rsvp-submit');
    await user.click(submitButton);

    // Should show success notification
    await waitFor(() => {
      expect(screen.getByText(/RSVP removed successfully/i)).toBeInTheDocument();
    });
  });

  it('shows success notification after ticket refund', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Bob Builder')).toBeInTheDocument();
    });

    // Click Refund link
    const refundLink = screen.getByTestId('refund-ticket-participation-2');
    await user.click(refundLink);

    // Wait for modal
    await waitFor(() => {
      expect(screen.getByTestId('refund-ticket-modal')).toBeInTheDocument();
    });

    // Check confirmation checkbox
    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');
    await user.click(confirmationCheckbox);

    // Click submit
    const submitButton = screen.getByTestId('refund-ticket-submit');
    await user.click(submitButton);

    // Should show success notification
    await waitFor(() => {
      expect(screen.getByText(/Ticket refunded successfully/i)).toBeInTheDocument();
    });
  });

  it('invalidates query cache after RSVP removal', async () => {
    const user = userEvent.setup();
    const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries');

    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Charlie Chaplin')).toBeInTheDocument();
    });

    // Click Remove link and confirm
    const removeLink = screen.getByTestId('remove-rsvp-participation-3');
    await user.click(removeLink);

    await waitFor(() => {
      expect(screen.getByTestId('remove-rsvp-modal')).toBeInTheDocument();
    });

    const confirmationCheckbox = screen.getByTestId('remove-rsvp-confirmation');
    await user.click(confirmationCheckbox);

    const submitButton = screen.getByTestId('remove-rsvp-submit');
    await user.click(submitButton);

    // Should invalidate participations query
    await waitFor(() => {
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({
        queryKey: ['events', 'event-123', 'participations']
      });
    });
  });

  it('invalidates query cache after ticket refund', async () => {
    const user = userEvent.setup();
    const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries');

    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Bob Builder')).toBeInTheDocument();
    });

    // Click Refund link and confirm
    const refundLink = screen.getByTestId('refund-ticket-participation-2');
    await user.click(refundLink);

    await waitFor(() => {
      expect(screen.getByTestId('refund-ticket-modal')).toBeInTheDocument();
    });

    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');
    await user.click(confirmationCheckbox);

    const submitButton = screen.getByTestId('refund-ticket-submit');
    await user.click(submitButton);

    // Should invalidate participations query
    await waitFor(() => {
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({
        queryKey: ['events', 'event-123', 'participations']
      });
    });
  });

  it('modal closes after successful RSVP removal', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Charlie Chaplin')).toBeInTheDocument();
    });

    // Click Remove link
    const removeLink = screen.getByTestId('remove-rsvp-participation-3');
    await user.click(removeLink);

    // Confirm modal is open
    await waitFor(() => {
      expect(screen.getByTestId('remove-rsvp-modal')).toBeInTheDocument();
    });

    // Check confirmation and submit
    const confirmationCheckbox = screen.getByTestId('remove-rsvp-confirmation');
    await user.click(confirmationCheckbox);

    const submitButton = screen.getByTestId('remove-rsvp-submit');
    await user.click(submitButton);

    // Modal should close
    await waitFor(() => {
      expect(screen.queryByTestId('remove-rsvp-modal')).not.toBeInTheDocument();
    });
  });

  it('modal closes after successful ticket refund', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // Navigate to Attendees tab
    const attendeesTab = screen.getByRole('tab', { name: /attendees/i });
    await user.click(attendeesTab);

    // Wait for participations to load
    await waitFor(() => {
      expect(screen.getByText('Bob Builder')).toBeInTheDocument();
    });

    // Click Refund link
    const refundLink = screen.getByTestId('refund-ticket-participation-2');
    await user.click(refundLink);

    // Confirm modal is open
    await waitFor(() => {
      expect(screen.getByTestId('refund-ticket-modal')).toBeInTheDocument();
    });

    // Check confirmation and submit
    const confirmationCheckbox = screen.getByTestId('refund-ticket-confirmation');
    await user.click(confirmationCheckbox);

    const submitButton = screen.getByTestId('refund-ticket-submit');
    await user.click(submitButton);

    // Modal should close
    await waitFor(() => {
      expect(screen.queryByTestId('refund-ticket-modal')).not.toBeInTheDocument();
    });
  });
});
