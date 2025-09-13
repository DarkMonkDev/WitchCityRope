import React from 'react';
import { Modal, TextInput, NumberInput, Group, Button, Stack, Select } from '@mantine/core';
import { DateInput, TimeInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import type { EventSession } from './EventSessionsGrid';

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
      date: session?.date ? new Date(session.date) : new Date(),
      startTime: session?.startTime || '18:00',
      endTime: session?.endTime || '21:00',
      capacity: session?.capacity || 50,
      registeredCount: session?.registeredCount || 0,
    },
    validate: {
      sessionIdentifier: (value) => {
        if (!value) return 'Session identifier is required';
        if (!value.match(/^S\d+$/)) return 'Must be in format S1, S2, etc.';
        // Check for duplicates only if creating new or changing identifier
        if (!session || session.sessionIdentifier !== value) {
          const exists = existingSessions.some(s => s.sessionIdentifier === value);
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
      const sessionData: Omit<EventSession, 'id'> = {
        sessionIdentifier: values.sessionIdentifier,
        name: values.name,
        date: values.date instanceof Date ? values.date.toISOString() : values.date,
        startTime: values.startTime,
        endTime: values.endTime,
        capacity: values.capacity,
        registeredCount: values.registeredCount,
      };
      onSubmit(sessionData);
      form.reset();
      onClose();
    })(event);
  };

  // Generate session identifier suggestions
  const getNextSessionIdentifier = () => {
    const existingNumbers = existingSessions
      .map(s => parseInt(s.sessionIdentifier.replace('S', '')))
      .filter(n => !isNaN(n));
    const maxNumber = existingNumbers.length > 0 ? Math.max(...existingNumbers) : 0;
    return `S${maxNumber + 1}`;
  };

  React.useEffect(() => {
    if (opened) {
      if (session) {
        // Populate form with existing session data for editing
        form.setValues({
          sessionIdentifier: session.sessionIdentifier,
          name: session.name,
          date: session.date ? new Date(session.date) : new Date(),
          startTime: session.startTime,
          endTime: session.endTime,
          capacity: session.capacity,
          registeredCount: session.registeredCount,
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

          <DateInput
            label="Date"
            placeholder="Select date"
            required
            minDate={new Date()}
            data-testid="input-session-date"
            {...form.getInputProps('date')}
          />

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

          <Group grow>
            <NumberInput
              label="Capacity"
              placeholder="Maximum attendees"
              min={1}
              max={1000}
              required
              data-testid="input-session-capacity"
              {...form.getInputProps('capacity')}
            />
            <NumberInput
              label="Already Registered"
              placeholder="Current registrations"
              min={0}
              disabled={!session} // Only editable for existing sessions
              {...form.getInputProps('registeredCount')}
            />
          </Group>

          <Group justify="flex-end" mt="md">
            <Button variant="outline" onClick={onClose} data-testid="button-cancel-session">
              Cancel
            </Button>
            <Button
              type="submit"
              data-testid="button-save-session"
              style={{
                background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
                border: 'none',
                color: 'var(--mantine-color-dark-9)',
                fontWeight: 600,
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