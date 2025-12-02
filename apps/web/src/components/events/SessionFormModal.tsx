import React from 'react';
import { Modal, TextInput, NumberInput, Group, Button, Stack, Select, Checkbox } from '@mantine/core';
import { TimeInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import type { EventSession } from './EventSessionsGrid';
import { StyledDatePicker } from '../forms/StyledDatePicker';
import { localTimeStringToUtc, utcToLocal, DEFAULT_EVENT_TIMEZONE } from '@/utils/eventUtils';

interface SessionFormModalProps {
  opened: boolean;
  onClose: () => void;
  onSubmit: (session: Omit<EventSession, 'id'>) => void;
  session?: EventSession | null;
  existingSessions: EventSession[];
}

export const SessionFormModal: React.FC<SessionFormModalProps> = ({
  opened,
  onClose,
  onSubmit,
  session,
  existingSessions,
}) => {
  const form = useForm({
    initialValues: {
      sessionIdentifier: session?.sessionIdentifier || '',
      name: session?.name || '',
      // Parse date from UTC ISO string as local date to prevent timezone shift
      date: session?.date ? (() => {
        const datePart = session.date.split('T')[0]; // Extract YYYY-MM-DD
        const [year, month, day] = datePart.split('-').map(Number);
        return new Date(year, month - 1, day); // Create local date
      })() : new Date(),
      // Check if session spans multiple days (endDate exists and is different from date)
      spansMultipleDays: session?.endDate ? (() => {
        const startDatePart = session.date?.split('T')[0];
        const endDatePart = session.endDate?.split('T')[0];
        return startDatePart !== endDatePart;
      })() : false,
      // Parse end date from UTC ISO string as local date
      endDate: session?.endDate ? (() => {
        const datePart = session.endDate.split('T')[0]; // Extract YYYY-MM-DD
        const [year, month, day] = datePart.split('-').map(Number);
        return new Date(year, month - 1, day); // Create local date
      })() : new Date(),
      startTime: session?.startTime || '18:00',
      endTime: session?.endTime || '21:00',
      capacity: session?.capacity || 50,
      registrationCount: session?.registrationCount || 0,
    },
    validate: {
      sessionIdentifier: (value) => {
        if (!value) return 'Session identifier is required';
        if (!value.match(/^S\d+$/)) return 'Must be in format S1, S2, etc.';
        // Check for duplicates only if creating new or changing identifier
        if (!session || session.sessionIdentifier !== value) {
          const exists = existingSessions.some(s => s?.sessionIdentifier === value);
          if (exists) return 'Session identifier already exists';
        }
        return null;
      },
      name: (value) => (!value ? 'Session name is required' : null),
      capacity: (value) => {
        if (!value || value < 1) return 'Capacity must be at least 1';
        if (value > 1000) return 'Capacity cannot exceed 1000';
        return null;
      },
      endDate: (value, values) => {
        // Only validate if session spans multiple days
        if (!values.spansMultipleDays) return null;
        if (!value) return 'End date is required for multi-day sessions';

        // Ensure end date is >= start date
        const startDate = values.date instanceof Date ? values.date : new Date(values.date);
        const endDate = value instanceof Date ? value : new Date(value);

        if (endDate < startDate) {
          return 'End date must be on or after start date';
        }
        return null;
      },
      startTime: (value, values) => {
        if (!value) return 'Start time is required';
        // For multi-day sessions, time comparison needs to consider the full datetime
        // For same-day, simple string comparison works (e.g., "18:00" < "21:00")
        if (!values.spansMultipleDays && values.endTime && value >= values.endTime) {
          return 'Start time must be before end time';
        }
        return null;
      },
      endTime: (value, values) => {
        if (!value) return 'End time is required';
        // For multi-day sessions, time comparison needs to consider the full datetime
        // For same-day, simple string comparison works
        if (!values.spansMultipleDays && values.startTime && value <= values.startTime) {
          return 'End time must be after start time';
        }
        return null;
      },
    },
  });

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); // Prevent default form submission that causes page refresh

    form.onSubmit((values) => {
      // Convert date to proper Date object if it's not already
      const sessionDate = values.date instanceof Date ? values.date : new Date(values.date || new Date());

      // Extract date components for the calendar date (no timezone conversion here)
      const year = sessionDate.getFullYear();
      const month = sessionDate.getMonth();
      const day = sessionDate.getDate();

      // Create a clean date object for the calendar date
      const calendarDate = new Date(year, month, day);

      // Determine the end date for time calculation
      let endCalendarDate: Date;
      let endDateValue: string;

      if (values.spansMultipleDays && values.endDate) {
        // Multi-day session: use the specified end date
        const sessionEndDate = values.endDate instanceof Date ? values.endDate : new Date(values.endDate);
        const endYear = sessionEndDate.getFullYear();
        const endMonth = sessionEndDate.getMonth();
        const endDay = sessionEndDate.getDate();
        endCalendarDate = new Date(endYear, endMonth, endDay);
        endDateValue = new Date(endYear, endMonth, endDay, 0, 0, 0, 0).toISOString();
      } else {
        // Single day session: end date equals start date
        endCalendarDate = calendarDate;
        endDateValue = new Date(year, month, day, 0, 0, 0, 0).toISOString();
      }

      // Convert local times to TRUE UTC using the event timezone
      // This correctly handles EST/EDT offset (e.g., 6 PM EST → 11 PM UTC)
      // IMPORTANT: endDateTime uses endCalendarDate to handle multi-day sessions correctly
      const startDateTime = localTimeStringToUtc(calendarDate, values.startTime, DEFAULT_EVENT_TIMEZONE);
      const endDateTime = localTimeStringToUtc(endCalendarDate, values.endTime, DEFAULT_EVENT_TIMEZONE);

      const sessionData: Omit<EventSession, 'id'> = {
        sessionIdentifier: values.sessionIdentifier,
        name: values.name,
        date: new Date(year, month, day, 0, 0, 0, 0).toISOString(), // Calendar date at midnight local
        endDate: endDateValue, // End date at midnight local
        startTime: startDateTime, // True UTC ISO string
        endTime: endDateTime,     // True UTC ISO string
        capacity: values.capacity,
        registrationCount: values.registrationCount,
      };

      onSubmit(sessionData);
      form.reset();
      onClose();
    })(event);
  };

  // Generate session identifier suggestions
  const getNextSessionIdentifier = () => {
    const existingNumbers = existingSessions
      .map(s => {
        // Safety check: ensure sessionIdentifier exists and is a string
        if (!s?.sessionIdentifier || typeof s.sessionIdentifier !== 'string') {
          return NaN;
        }
        return parseInt(s.sessionIdentifier.replace('S', ''));
      })
      .filter(n => !isNaN(n));
    const maxNumber = existingNumbers.length > 0 ? Math.max(...existingNumbers) : 0;
    return `S${maxNumber + 1}`;
  };

  React.useEffect(() => {
    if (opened) {
      if (session) {
        // Populate form with existing session data for editing
        // Convert stored UTC times back to local time for display in form fields
        const startLocal = utcToLocal(session.startTime, DEFAULT_EVENT_TIMEZONE);
        const endLocal = utcToLocal(session.endTime, DEFAULT_EVENT_TIMEZONE);

        // Use the 24-hour format time string for the TimeInput component
        const startTimeString = startLocal.time24;
        const endTimeString = endLocal.time24;

        // Parse date from UTC ISO string as local date to prevent timezone shift
        const dateValue = session.date ? (() => {
          const datePart = session.date.split('T')[0]; // Extract YYYY-MM-DD
          const [year, month, day] = datePart.split('-').map(Number);
          return new Date(year, month - 1, day); // Create local date
        })() : new Date();

        // Parse end date and check if session spans multiple days
        const endDateValue = session.endDate ? (() => {
          const datePart = session.endDate.split('T')[0]; // Extract YYYY-MM-DD
          const [year, month, day] = datePart.split('-').map(Number);
          return new Date(year, month - 1, day); // Create local date
        })() : dateValue;

        const startDatePart = session.date?.split('T')[0];
        const endDatePart = session.endDate?.split('T')[0];
        const spansMultipleDays = session.endDate ? startDatePart !== endDatePart : false;

        form.setValues({
          sessionIdentifier: session.sessionIdentifier,
          name: session.name,
          date: dateValue,
          endDate: endDateValue,
          spansMultipleDays,
          startTime: startTimeString,
          endTime: endTimeString,
          capacity: session.capacity,
          registrationCount: session.registrationCount,
        });
      } else {
        // Reset form for new session and auto-suggest next session identifier
        form.reset();
        form.setFieldValue('sessionIdentifier', getNextSessionIdentifier());
      }
    }
  }, [opened, session]);

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={session ? 'Edit Session' : 'Add Session'}
      size="md"
      centered
      data-testid={session ? "modal-edit-session" : "modal-add-session"}
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <Group grow>
            <Select
              label="Session Identifier"
              placeholder="S1, S2, S3..."
              data={[
                { value: 'S1', label: 'S1 - Session 1' },
                { value: 'S2', label: 'S2 - Session 2' },
                { value: 'S3', label: 'S3 - Session 3' },
                { value: 'S4', label: 'S4 - Session 4' },
                { value: 'S5', label: 'S5 - Session 5' },
              ]}
              searchable
              required
              data-testid="input-session-id"
              {...form.getInputProps('sessionIdentifier')}
            />
            <TextInput
              label="Session Name"
              placeholder="e.g., Morning Workshop"
              required
              data-testid="input-session-name"
              {...form.getInputProps('name')}
            />
          </Group>

          <Group grow>
            <StyledDatePicker
              label="Start Date"
              placeholder="Select start date"
              required
              minDate={new Date()}
              data-testid="input-session-date"
              {...form.getInputProps('date')}
            />
            <NumberInput
              label="Capacity"
              placeholder="Maximum attendees"
              min={1}
              max={1000}
              required
              data-testid="input-session-capacity"
              {...form.getInputProps('capacity')}
            />
          </Group>

          <Checkbox
            label="Session spans multiple days"
            data-testid="checkbox-spans-multiple-days"
            {...form.getInputProps('spansMultipleDays', { type: 'checkbox' })}
          />

          {form.values.spansMultipleDays && (
            <StyledDatePicker
              label="End Date"
              placeholder="Select end date"
              required
              minDate={form.values.date}
              data-testid="input-session-end-date"
              {...form.getInputProps('endDate')}
            />
          )}

          <Group grow>
            <TimeInput
              label="Start Time"
              placeholder="HH:MM"
              required
              data-testid="input-session-start-time"
              {...form.getInputProps('startTime')}
            />
            <TimeInput
              label="End Time"
              placeholder="HH:MM"
              required
              data-testid="input-session-end-time"
              {...form.getInputProps('endTime')}
            />
          </Group>

          <Group justify="flex-end" mt="md">
            <Button variant="outline" onClick={onClose} data-testid="button-cancel-session">
              Cancel
            </Button>
            <Button
              type="submit"
              data-testid="button-save-session"
              styles={{
                root: {
                  background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
                  border: 'none',
                  color: 'var(--mantine-color-dark-9)',
                  fontWeight: 600,
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                }
              }}
            >
              {session ? 'Update Session' : 'Add Session'}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
};