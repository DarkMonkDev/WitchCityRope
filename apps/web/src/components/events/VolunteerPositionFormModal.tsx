import React from 'react';
import { Modal, TextInput, Textarea, NumberInput, Group, Button, Stack, Select } from '@mantine/core';
import { TimeInput } from '@mantine/dates';
import { useForm } from '@mantine/form';

export interface VolunteerPosition {
  id: string;
  positionName: string;
  description: string;
  sessions: string; // "S1, S2" or "All Sessions"
  startTime: string;
  endTime: string;
  volunteersNeeded: number;
  volunteersAssigned: number;
}

interface VolunteerPositionFormModalProps {
  opened: boolean;
  onClose: () => void;
  onSubmit: (position: Omit<VolunteerPosition, 'id' | 'volunteersAssigned'>) => void;
  position?: VolunteerPosition | null;
  availableSessions: Array<{ sessionIdentifier: string; name: string }>;
}

export const VolunteerPositionFormModal: React.FC<VolunteerPositionFormModalProps> = ({
  opened,
  onClose,
  onSubmit,
  position,
  availableSessions,
}) => {
  const form = useForm({
    initialValues: {
      positionName: position?.positionName || '',
      description: position?.description || '',
      sessions: position?.sessions || 'All Sessions',
      startTime: position?.startTime || '18:00',
      endTime: position?.endTime || '21:00',
      volunteersNeeded: position?.volunteersNeeded || 1,
    },
    validate: {
      positionName: (value) => (!value ? 'Position name is required' : null),
      description: (value) => (!value ? 'Description is required' : null),
      volunteersNeeded: (value) => {
        if (!value || value < 1) return 'Must need at least 1 volunteer';
        if (value > 20) return 'Cannot exceed 20 volunteers per position';
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
      const positionData: Omit<VolunteerPosition, 'id' | 'volunteersAssigned'> = {
        positionName: values.positionName,
        description: values.description,
        sessions: values.sessions,
        startTime: values.startTime,
        endTime: values.endTime,
        volunteersNeeded: values.volunteersNeeded,
      };
      onSubmit(positionData);
      form.reset();
      onClose();
    })(event);
  };

  // Generate session options from available sessions with safety checks
  const sessionOptions = [
    { value: 'All Sessions', label: 'All Sessions' },
    ...availableSessions
      .filter(session => session?.sessionIdentifier && session?.name) // Filter out invalid sessions
      .map(session => ({
        value: session.sessionIdentifier,
        label: `${session.sessionIdentifier} - ${session.name}`,
      })),
  ];

  // Reset form when modal opens
  React.useEffect(() => {
    if (opened) {
      if (position) {
        form.setValues({
          positionName: position.positionName,
          description: position.description,
          sessions: position.sessions,
          startTime: position.startTime,
          endTime: position.endTime,
          volunteersNeeded: position.volunteersNeeded,
        });
      } else {
        form.reset();
      }
    }
  }, [opened, position]);

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={position ? 'Edit Volunteer Position' : 'Add Volunteer Position'}
      size="md"
      centered
      data-testid={position ? "modal-edit-volunteer-position" : "modal-add-volunteer-position"}
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <TextInput
            label="Position Name"
            placeholder="e.g., Safety Monitor, Door Greeter"
            required
            data-testid="input-position-name"
            {...form.getInputProps('positionName')}
          />

          <Textarea
            label="Description"
            placeholder="Describe the volunteer duties and responsibilities..."
            required
            minRows={3}
            maxRows={6}
            data-testid="textarea-position-description"
            {...form.getInputProps('description')}
          />

          <Select
            label="Sessions"
            placeholder="Select which sessions this position covers"
            data={sessionOptions}
            required
            data-testid="dropdown-position-sessions"
            {...form.getInputProps('sessions')}
          />

          <Group grow>
            <TimeInput
              label="Start Time"
              placeholder="HH:MM"
              required
              data-testid="input-position-start-time"
              {...form.getInputProps('startTime')}
            />
            <TimeInput
              label="End Time"
              placeholder="HH:MM"
              required
              data-testid="input-position-end-time"
              {...form.getInputProps('endTime')}
            />
          </Group>

          <NumberInput
            label="Volunteers Needed"
            placeholder="Number of volunteers required"
            min={1}
            max={20}
            required
            data-testid="input-volunteers-needed"
            {...form.getInputProps('volunteersNeeded')}
          />

          <Group justify="flex-end" mt="md">
            <Button variant="outline" onClick={onClose} data-testid="button-cancel-volunteer-position">
              Cancel
            </Button>
            <Button
              type="submit"
              style={{
                background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
                border: 'none',
                color: 'var(--mantine-color-dark-9)',
                fontWeight: 600,
              }}
              data-testid="button-save-volunteer-position"
            >
              {position ? 'Update Position' : 'Add Position'}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
};