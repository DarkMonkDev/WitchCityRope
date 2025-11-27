import React from 'react';
import { Modal, TextInput, NumberInput, Group, Button, Stack, Select } from '@mantine/core';
import { TimeInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import type { EventSession } from './EventSessionsGrid';
import { StyledDatePicker } from '../forms/StyledDatePicker';

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
      startTime: (value, values) => {
        if (!value) return 'Start time is required';
        // Check that end time is after start time
        if (values.endTime && value >= values.endTime) {
          return 'Start time must be before end time';
        }
        return null;
      },
      endTime: (value, values) => {
        if (!value) return 'End time is required';
        if (values.startTime && value <= values.startTime) {
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

      // Extract date components using UTC methods to avoid timezone shifts
      const year = sessionDate.getUTCFullYear();
      const month = sessionDate.getUTCMonth();
      const day = sessionDate.getUTCDate();

      // Parse time strings
      const [startHour, startMinute] = values.startTime.split(':').map(Number);
      const [endHour, endMinute] = values.endTime.split(':').map(Number);

      // Create datetime objects using Date.UTC() to ensure UTC timezone
      const startDateTime = new Date(Date.UTC(year, month, day, startHour, startMinute, 0, 0));
      const endDateTime = new Date(Date.UTC(year, month, day, endHour, endMinute, 0, 0));

      const sessionData: Omit<EventSession, 'id'> = {
        sessionIdentifier: values.sessionIdentifier,
        name: values.name,
        date: new Date(Date.UTC(year, month, day, 0, 0, 0, 0)).toISOString(),
        startTime: startDateTime.toISOString(), // Full ISO datetime string
        endTime: endDateTime.toISOString(),     // Full ISO datetime string
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
        // Extract time portion from UTC ISO datetime strings using UTC methods
        const startDate = new Date(session.startTime);
        const endDate = new Date(session.endTime);
        const startTimeString = `${startDate.getUTCHours().toString().padStart(2, '0')}:${startDate.getUTCMinutes().toString().padStart(2, '0')}`;
        const endTimeString = `${endDate.getUTCHours().toString().padStart(2, '0')}:${endDate.getUTCMinutes().toString().padStart(2, '0')}`;

        // Parse date from UTC ISO string as local date to prevent timezone shift
        const dateValue = session.date ? (() => {
          const datePart = session.date.split('T')[0]; // Extract YYYY-MM-DD
          const [year, month, day] = datePart.split('-').map(Number);
          return new Date(year, month - 1, day); // Create local date
        })() : new Date();

        form.setValues({
          sessionIdentifier: session.sessionIdentifier,
          name: session.name,
          date: dateValue,
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
              label="Date"
              placeholder="Select date"
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