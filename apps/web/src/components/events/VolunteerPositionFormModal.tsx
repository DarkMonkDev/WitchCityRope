/**
 * @deprecated This modal component is deprecated as of 2025-10-21.
 * Use inline editing in VolunteerPositionsGrid instead.
 * This file is kept for the VolunteerPosition interface export only.
 *
 * @see VolunteerPositionsGrid - New inline editing implementation
 * @see VolunteerPositionInlineForm - Replacement form component
 */
import React from 'react';
import { Modal, TextInput, Textarea, NumberInput, Group, Button, Stack, Select } from '@mantine/core';
import { TimeInput } from '@mantine/dates';
import { useForm } from '@mantine/form';

export interface VolunteerPosition {
  id: string;
  title: string;  // Match API field name
  description: string;
  sessions: string; // "S1, S2" or "All Sessions"
  startTime: string;
  endTime: string;
  slotsNeeded: number;  // Match API field name
  slotsFilled: number;  // Match API field name
  isPublicFacing: boolean;  // Whether visible on public event page
}

interface VolunteerPositionFormModalProps {
  opened: boolean;
  onClose: () => void;
  onSubmit: (position: Omit<VolunteerPosition, 'id' | 'slotsFilled'>) => void;
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
      title: position?.title || '',
      description: position?.description || '',
      sessions: position?.sessions || (availableSessions[0]?.sessionIdentifier || 'S1'),
      startTime: position?.startTime || '18:00',
      endTime: position?.endTime || '21:00',
      slotsNeeded: position?.slotsNeeded || 1,
    },
    validate: {
      title: (value) => (!value ? 'Position title is required' : null),
      description: (value) => (!value ? 'Description is required' : null),
      slotsNeeded: (value) => {
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
      const positionData: Omit<VolunteerPosition, 'id' | 'slotsFilled'> = {
        title: values.title,
        description: values.description,
        sessions: values.sessions,
        startTime: values.startTime,
        endTime: values.endTime,
        slotsNeeded: values.slotsNeeded,
      };
      onSubmit(positionData);
      form.reset();
      onClose();
    })(event);
  };

  // Generate session options from available sessions (no "All Sessions" - each position must be assigned to a specific session)
  const sessionOptions = availableSessions
    .filter(session => session?.sessionIdentifier && session?.name) // Filter out invalid sessions
    .map(session => ({
      value: session.sessionIdentifier,
      label: `${session.sessionIdentifier} - ${session.name}`,
    }));

  // Reset form when modal opens
  React.useEffect(() => {
    if (opened) {
      if (position) {
        form.setValues({
          title: position.title,
          description: position.description,
          sessions: position.sessions,
          startTime: position.startTime,
          endTime: position.endTime,
          slotsNeeded: position.slotsNeeded,
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
            label="Position Title"
            placeholder="e.g., Safety Monitor, Door Greeter"
            required
            data-testid="input-position-title"
            {...form.getInputProps('title')}
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
            label="Slots Needed"
            placeholder="Number of volunteer slots required"
            min={1}
            max={20}
            required
            data-testid="input-slots-needed"
            {...form.getInputProps('slotsNeeded')}
          />

          <Group justify="flex-end" mt="md">
            <Button variant="outline" onClick={onClose} data-testid="button-cancel-volunteer-position">
              Cancel
            </Button>
            <Button
              type="submit"
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