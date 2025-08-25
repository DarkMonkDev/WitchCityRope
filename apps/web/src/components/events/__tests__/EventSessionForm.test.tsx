import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { EventSessionForm } from '../EventSessionForm';
import type { EventSession, TicketType } from '@witchcityrope/shared-types';

// TDD Test for Event Session Matrix Form Components
// Following React testing patterns from lessons learned

describe('EventSessionForm', () => {
  let queryClient: QueryClient;

  const createWrapper = () => {
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
    });
    
    return ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        <MantineProvider>
          {children}
        </MantineProvider>
      </QueryClientProvider>
    );
  };

  const mockEventSessions: EventSession[] = [
    {
      id: '1',
      eventId: 'event-1',
      sessionName: 'S1',
      sessionDate: '2025-08-31',
      startTime: '09:00',
      endTime: '12:00',
      capacity: 20,
      isRequired: false,
      createdAt: '2025-08-24T10:00:00Z',
      updatedAt: '2025-08-24T10:00:00Z'
    },
    {
      id: '2', 
      eventId: 'event-1',
      sessionName: 'S2',
      sessionDate: '2025-09-01', 
      startTime: '09:00',
      endTime: '12:00',
      capacity: 25,
      isRequired: false,
      createdAt: '2025-08-24T10:00:00Z',
      updatedAt: '2025-08-24T10:00:00Z'
    },
    {
      id: '3',
      eventId: 'event-1', 
      sessionName: 'S3',
      sessionDate: '2025-09-02',
      startTime: '09:00',
      endTime: '12:00', 
      capacity: 18,
      isRequired: false,
      createdAt: '2025-08-24T10:00:00Z',
      updatedAt: '2025-08-24T10:00:00Z'
    }
  ];

  const mockTicketTypes: TicketType[] = [
    {
      id: '1',
      eventId: 'event-1',
      name: 'Full Series Pass',
      description: 'Access to all 3 sessions',
      price: 150.00,
      maxQuantity: null,
      saleStartDate: null,
      saleEndDate: null,
      isActive: true,
      sortOrder: 0,
      includedSessions: ['S1', 'S2', 'S3'],
      createdAt: '2025-08-24T10:00:00Z',
      updatedAt: '2025-08-24T10:00:00Z'
    },
    {
      id: '2',
      eventId: 'event-1',
      name: 'Weekend Pass',
      description: 'Saturday and Sunday sessions',
      price: 100.00,
      maxQuantity: null,
      saleStartDate: null,
      saleEndDate: null,
      isActive: true,
      sortOrder: 1,
      includedSessions: ['S2', 'S3'],
      createdAt: '2025-08-24T10:00:00Z',
      updatedAt: '2025-08-24T10:00:00Z'
    }
  ];

  const defaultProps = {
    eventId: 'event-1',
    sessions: mockEventSessions,
    ticketTypes: mockTicketTypes,
    onSave: vi.fn(),
    onCancel: vi.fn()
  };

  describe('Session Management', () => {
    it('should display all event sessions with capacity information', () => {
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Assert sessions are displayed
      expect(screen.getByText('S1 - Friday Workshop')).toBeInTheDocument();
      expect(screen.getByText('Capacity: 20')).toBeInTheDocument();
      
      expect(screen.getByText('S2 - Saturday Workshop')).toBeInTheDocument();
      expect(screen.getByText('Capacity: 25')).toBeInTheDocument();
      
      expect(screen.getByText('S3 - Sunday Workshop')).toBeInTheDocument();
      expect(screen.getByText('Capacity: 18')).toBeInTheDocument();
    });

    it('should allow adding new session with validation', async () => {
      const user = userEvent.setup();
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Click add session button
      await user.click(screen.getByRole('button', { name: /add session/i }));

      // Fill in new session form
      await user.type(screen.getByLabelText(/session name/i), 'S4');
      await user.type(screen.getByLabelText(/session date/i), '2025-09-03');
      await user.type(screen.getByLabelText(/start time/i), '14:00');
      await user.type(screen.getByLabelText(/end time/i), '17:00');
      await user.type(screen.getByLabelText(/capacity/i), '22');

      // Submit new session
      await user.click(screen.getByRole('button', { name: /save session/i }));

      // Assert session was added to form
      await waitFor(() => {
        expect(screen.getByText('S4 - New Session')).toBeInTheDocument();
        expect(screen.getByText('Capacity: 22')).toBeInTheDocument();
      });
    });

    it('should validate session capacity is greater than zero', async () => {
      const user = userEvent.setup();
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      await user.click(screen.getByRole('button', { name: /add session/i }));
      
      // Enter invalid capacity
      const capacityInput = screen.getByLabelText(/capacity/i);
      await user.clear(capacityInput);
      await user.type(capacityInput, '0');
      
      await user.click(screen.getByRole('button', { name: /save session/i }));

      // Assert validation error
      await waitFor(() => {
        expect(screen.getByText(/capacity must be greater than zero/i)).toBeInTheDocument();
      });
    });

    it('should prevent overlapping session times on same date', async () => {
      const user = userEvent.setup();
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      await user.click(screen.getByRole('button', { name: /add session/i }));
      
      // Try to create overlapping session with S1 (09:00-12:00 on 2025-08-31)
      await user.type(screen.getByLabelText(/session name/i), 'S4');
      await user.type(screen.getByLabelText(/session date/i), '2025-08-31'); // Same date as S1
      await user.type(screen.getByLabelText(/start time/i), '11:00'); // Overlaps S1
      await user.type(screen.getByLabelText(/end time/i), '14:00');
      await user.type(screen.getByLabelText(/capacity/i), '15');

      await user.click(screen.getByRole('button', { name: /save session/i }));

      // Assert validation error
      await waitFor(() => {
        expect(screen.getByText(/sessions cannot overlap on the same date/i)).toBeInTheDocument();
      });
    });
  });

  describe('Ticket Type Session Mapping', () => {
    it('should display ticket types with session mappings', () => {
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Assert ticket types are displayed with session mapping
      expect(screen.getByText('Full Series Pass')).toBeInTheDocument();
      expect(screen.getByText('Includes: S1, S2, S3')).toBeInTheDocument();
      expect(screen.getByText('$150.00')).toBeInTheDocument();

      expect(screen.getByText('Weekend Pass')).toBeInTheDocument();
      expect(screen.getByText('Includes: S2, S3')).toBeInTheDocument();
      expect(screen.getByText('$100.00')).toBeInTheDocument();
    });

    it('should allow creating ticket type with session selection', async () => {
      const user = userEvent.setup();
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Click add ticket type
      await user.click(screen.getByRole('button', { name: /add ticket type/i }));

      // Fill in ticket type form
      await user.type(screen.getByLabelText(/ticket name/i), 'Friday Only Pass');
      await user.type(screen.getByLabelText(/description/i), 'Access to Friday session only');
      await user.type(screen.getByLabelText(/price/i), '60');

      // Select sessions - check S1 only
      const s1Checkbox = screen.getByLabelText(/S1 - Friday Workshop/i);
      await user.click(s1Checkbox);

      await user.click(screen.getByRole('button', { name: /save ticket type/i }));

      // Assert ticket type was added
      await waitFor(() => {
        expect(screen.getByText('Friday Only Pass')).toBeInTheDocument();
        expect(screen.getByText('Includes: S1')).toBeInTheDocument();
        expect(screen.getByText('$60.00')).toBeInTheDocument();
      });
    });

    it('should validate ticket type must include at least one session', async () => {
      const user = userEvent.setup();
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      await user.click(screen.getByRole('button', { name: /add ticket type/i }));
      
      // Fill form but don't select any sessions
      await user.type(screen.getByLabelText(/ticket name/i), 'No Sessions Ticket');
      await user.type(screen.getByLabelText(/price/i), '50');

      await user.click(screen.getByRole('button', { name: /save ticket type/i }));

      // Assert validation error
      await waitFor(() => {
        expect(screen.getByText(/ticket type must include at least one session/i)).toBeInTheDocument();
      });
    });

    it('should prevent mapping to non-existent sessions', async () => {
      const user = userEvent.setup();
      
      // Props with ticket type referencing non-existent session
      const propsWithInvalidTicket = {
        ...defaultProps,
        ticketTypes: [{
          ...mockTicketTypes[0],
          includedSessions: ['S1', 'S2', 'S4'] // S4 doesn't exist
        }]
      };

      render(<EventSessionForm {...propsWithInvalidTicket} />, { wrapper: createWrapper() });

      // Should show error for invalid session mapping
      expect(screen.getByText(/session 'S4' does not exist/i)).toBeInTheDocument();
    });
  });

  describe('Capacity Calculations', () => {
    it('should display availability calculations for each ticket type', () => {
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Full Series Pass availability should be limited by smallest session (S3: 18)
      expect(screen.getByText('Available: 18')).toBeInTheDocument(); // Limited by S3

      // Weekend Pass availability should be limited by S3 (18)  
      const weekendElements = screen.getAllByText('Available: 18');
      expect(weekendElements).toHaveLength(2); // Both Full Series and Weekend Pass
    });

    it('should update availability when session capacity changes', async () => {
      const user = userEvent.setup();
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Edit S3 session to reduce capacity
      const s3EditButton = screen.getByRole('button', { name: /edit s3/i });
      await user.click(s3EditButton);

      // Change capacity from 18 to 10
      const capacityInput = screen.getByDisplayValue('18');
      await user.clear(capacityInput);
      await user.type(capacityInput, '10');

      await user.click(screen.getByRole('button', { name: /save changes/i }));

      // Assert availability updates
      await waitFor(() => {
        expect(screen.getByText('Available: 10')).toBeInTheDocument(); // Now limited by S3: 10
      });
    });

    it('should show capacity warnings when sessions are at different limits', () => {
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Should show warning about capacity imbalance
      expect(screen.getByText(/capacity imbalance detected/i)).toBeInTheDocument();
      expect(screen.getByText(/S3 \(18\) limits Full Series Pass availability/i)).toBeInTheDocument();
    });
  });

  describe('RSVP vs Ticket Mode', () => {
    it('should show RSVP toggle for social events', () => {
      const socialEventProps = {
        ...defaultProps,
        eventType: 'social' as const
      };

      render(<EventSessionForm {...socialEventProps} />, { wrapper: createWrapper() });

      // Should show RSVP mode toggle
      expect(screen.getByLabelText(/enable free rsvp mode/i)).toBeInTheDocument();
      expect(screen.getByText(/social events can use free rsvp instead of paid tickets/i)).toBeInTheDocument();
    });

    it('should hide payment fields when RSVP mode is enabled', async () => {
      const user = userEvent.setup();
      const socialEventProps = {
        ...defaultProps,
        eventType: 'social' as const
      };

      render(<EventSessionForm {...socialEventProps} />, { wrapper: createWrapper() });

      // Enable RSVP mode
      const rsvpToggle = screen.getByLabelText(/enable free rsvp mode/i);
      await user.click(rsvpToggle);

      // Add new ticket type
      await user.click(screen.getByRole('button', { name: /add ticket type/i }));

      // Price field should be hidden/disabled in RSVP mode
      expect(screen.queryByLabelText(/price/i)).not.toBeInTheDocument();
      expect(screen.getByText(/rsvp mode - no payment required/i)).toBeInTheDocument();
    });

    it('should not show RSVP toggle for class events', () => {
      const classEventProps = {
        ...defaultProps,
        eventType: 'class' as const
      };

      render(<EventSessionForm {...classEventProps} />, { wrapper: createWrapper() });

      // RSVP toggle should not be present for class events
      expect(screen.queryByLabelText(/enable free rsvp mode/i)).not.toBeInTheDocument();
      expect(screen.getByText(/class events require payment processing/i)).toBeInTheDocument();
    });
  });

  describe('Form Submission and Validation', () => {
    it('should call onSave with complete event session data', async () => {
      const user = userEvent.setup();
      const onSave = vi.fn();
      render(<EventSessionForm {...defaultProps} onSave={onSave} />, { wrapper: createWrapper() });

      // Click save button
      await user.click(screen.getByRole('button', { name: /save event configuration/i }));

      // Assert onSave called with correct data structure
      await waitFor(() => {
        expect(onSave).toHaveBeenCalledWith({
          sessions: expect.arrayContaining([
            expect.objectContaining({
              sessionName: 'S1',
              capacity: 20
            })
          ]),
          ticketTypes: expect.arrayContaining([
            expect.objectContaining({
              name: 'Full Series Pass',
              includedSessions: ['S1', 'S2', 'S3']
            })
          ])
        });
      });
    });

    it('should validate event has at least one session before saving', async () => {
      const user = userEvent.setup();
      const propsWithNoSessions = {
        ...defaultProps,
        sessions: []
      };

      render(<EventSessionForm {...propsWithNoSessions} />, { wrapper: createWrapper() });

      await user.click(screen.getByRole('button', { name: /save event configuration/i }));

      // Assert validation error
      await waitFor(() => {
        expect(screen.getByText(/event must have at least one session/i)).toBeInTheDocument();
      });
    });

    it('should validate event has at least one ticket type before saving', async () => {
      const user = userEvent.setup();
      const propsWithNoTickets = {
        ...defaultProps,
        ticketTypes: []
      };

      render(<EventSessionForm {...propsWithNoTickets} />, { wrapper: createWrapper() });

      await user.click(screen.getByRole('button', { name: /save event configuration/i }));

      // Assert validation error
      await waitFor(() => {
        expect(screen.getByText(/event must have at least one ticket type/i)).toBeInTheDocument();
      });
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels and roles', () => {
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Check form has proper labeling
      expect(screen.getByRole('form', { name: /event session configuration/i })).toBeInTheDocument();
      
      // Check sections have proper headings
      expect(screen.getByRole('heading', { name: /event sessions/i })).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: /ticket types/i })).toBeInTheDocument();
      
      // Check buttons have accessible names
      expect(screen.getByRole('button', { name: /add session/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /add ticket type/i })).toBeInTheDocument();
    });

    it('should support keyboard navigation', async () => {
      const user = userEvent.setup();
      render(<EventSessionForm {...defaultProps} />, { wrapper: createWrapper() });

      // Tab through form elements
      await user.tab();
      expect(screen.getByRole('button', { name: /add session/i })).toHaveFocus();
      
      await user.tab();
      expect(screen.getByRole('button', { name: /add ticket type/i })).toHaveFocus();

      await user.tab();
      expect(screen.getByRole('button', { name: /save event configuration/i })).toHaveFocus();
    });
  });
});