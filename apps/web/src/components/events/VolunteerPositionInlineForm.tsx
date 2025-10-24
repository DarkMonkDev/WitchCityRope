import React, { useEffect } from 'react';
import {
  Stack,
  TextInput,
  Textarea,
  NumberInput,
  Group,
  Button,
  Select,
  Switch,
  Box,
  Text
} from '@mantine/core';
import { TimeInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { IconDeviceFloppy, IconX, IconTrash } from '@tabler/icons-react';
import type { VolunteerPosition } from './VolunteerPositionFormModal';

interface VolunteerPositionInlineFormProps {
  position?: VolunteerPosition | null;
  onSubmit: (position: Omit<VolunteerPosition, 'id' | 'slotsFilled'>) => void;
  onCancel: () => void;
  onDelete?: (positionId: string) => void;
  availableSessions: Array<{ sessionIdentifier: string; name: string }>;
  mode: 'create' | 'edit';
}

export const VolunteerPositionInlineForm: React.FC<VolunteerPositionInlineFormProps> = ({
  position,
  onSubmit,
  onCancel,
  onDelete,
  availableSessions,
  mode,
}) => {
  const form = useForm({
    initialValues: {
      title: position?.title || '',
      description: position?.description || '',
      sessions: position?.sessions || 'All Sessions',
      startTime: position?.startTime || '18:00',
      endTime: position?.endTime || '21:00',
      slotsNeeded: position?.slotsNeeded || 1,
      isPublicFacing: position?.isPublicFacing !== undefined ? position.isPublicFacing : true,
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

  // Update form when position changes
  useEffect(() => {
    if (position) {
      form.setValues({
        title: position.title,
        description: position.description,
        sessions: position.sessions,
        startTime: position.startTime,
        endTime: position.endTime,
        slotsNeeded: position.slotsNeeded,
        isPublicFacing: position.isPublicFacing !== undefined ? position.isPublicFacing : true,
      });
    }
  }, [position]);

  const handleSubmit = form.onSubmit((values) => {
    const positionData: Omit<VolunteerPosition, 'id' | 'slotsFilled'> = {
      title: values.title,
      description: values.description,
      sessions: values.sessions,
      startTime: values.startTime,
      endTime: values.endTime,
      slotsNeeded: values.slotsNeeded,
      isPublicFacing: values.isPublicFacing,
    };
    onSubmit(positionData);
  });

  const handleDeleteClick = () => {
    if (position && onDelete) {
      if (window.confirm(`Are you sure you want to delete the "${position.title}" position?`)) {
        onDelete(position.id);
      }
    }
  };

  // Generate session options
  const sessionOptions = [
    { value: 'All Sessions', label: 'All Sessions' },
    ...availableSessions
      .filter(session => session?.sessionIdentifier && session?.name)
      .map(session => ({
        value: session.sessionIdentifier,
        label: `${session.sessionIdentifier} - ${session.name}`,
      })),
  ];

  return (
    <Box
      p="xl"
      style={{
        backgroundColor: '#f8f9fa',
        borderRadius: '12px',
        border: '2px solid #e9ecef',
      }}
      data-testid="volunteer-position-inline-form"
    >
      <div>
        <Stack gap="md">
          <Text size="lg" fw={600} c="burgundy" mb="sm">
            {mode === 'create' ? 'Add New Volunteer Position' : 'Edit Volunteer Position'}
          </Text>

          <TextInput
            label="Position Title"
            placeholder="e.g., Safety Monitor, Door Greeter"
            required
            data-testid="input-position-title"
            styles={{
              label: { fontWeight: 600, marginBottom: '8px' },
              input: {
                height: '44px',
                fontSize: '14px',
              }
            }}
            {...form.getInputProps('title')}
          />

          <Textarea
            label="Description"
            placeholder="Describe the volunteer duties and responsibilities..."
            required
            minRows={3}
            maxRows={6}
            data-testid="textarea-position-description"
            styles={{
              label: { fontWeight: 600, marginBottom: '8px' },
              input: {
                fontSize: '14px',
              }
            }}
            {...form.getInputProps('description')}
          />

          <Group grow>
            <Select
              label="Sessions"
              placeholder="Select which sessions this position covers"
              data={sessionOptions}
              required
              data-testid="dropdown-position-sessions"
              styles={{
                label: { fontWeight: 600, marginBottom: '8px' },
                input: {
                  height: '44px',
                  fontSize: '14px',
                }
              }}
              {...form.getInputProps('sessions')}
            />
            <NumberInput
              label="Slots Needed"
              placeholder="Number of volunteer slots required"
              min={1}
              max={20}
              required
              data-testid="input-slots-needed"
              styles={{
                label: { fontWeight: 600, marginBottom: '8px' },
                input: {
                  height: '44px',
                  fontSize: '14px',
                }
              }}
              {...form.getInputProps('slotsNeeded')}
            />
          </Group>

          <Group grow>
            <TimeInput
              label="Start Time"
              placeholder="HH:MM"
              required
              data-testid="input-position-start-time"
              styles={{
                label: { fontWeight: 600, marginBottom: '8px' },
                input: {
                  height: '44px',
                  fontSize: '14px',
                }
              }}
              {...form.getInputProps('startTime')}
            />
            <TimeInput
              label="End Time"
              placeholder="HH:MM"
              required
              data-testid="input-position-end-time"
              styles={{
                label: { fontWeight: 600, marginBottom: '8px' },
                input: {
                  height: '44px',
                  fontSize: '14px',
                }
              }}
              {...form.getInputProps('endTime')}
            />
          </Group>

          <Switch
            label="Public Facing Position"
            description="Visible to all members on the public event page"
            data-testid="switch-is-public-facing"
            styles={{
              label: { fontWeight: 600 },
              description: { fontSize: '12px', color: '#868e96' }
            }}
            {...form.getInputProps('isPublicFacing', { type: 'checkbox' })}
          />

          <Group justify="space-between" mt="md">
            <Group gap="xs">
              <Button
                variant="outline"
                color="gray"
                leftSection={<IconX size={16} />}
                onClick={onCancel}
                data-testid="button-cancel-volunteer-position"
                styles={{
                  root: {
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                    fontWeight: 600,
                  }
                }}
              >
                Cancel
              </Button>

              {mode === 'edit' && onDelete && position && (
                <Button
                  variant="filled"
                  color="red"
                  leftSection={<IconTrash size={16} />}
                  onClick={handleDeleteClick}
                  data-testid="button-delete-volunteer-position"
                  styles={{
                    root: {
                      height: '44px',
                      paddingTop: '12px',
                      paddingBottom: '12px',
                      fontSize: '14px',
                      lineHeight: '1.2',
                      fontWeight: 600,
                    }
                  }}
                >
                  Delete
                </Button>
              )}
            </Group>

            <Button
              onClick={handleSubmit}
              variant="filled"
              color="blue"
              leftSection={<IconDeviceFloppy size={16} />}
              data-testid="button-save-volunteer-position"
              styles={{
                root: {
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                  fontWeight: 600,
                }
              }}
            >
              {mode === 'create' ? 'Add Position' : 'Update Position'}
            </Button>
          </Group>
        </Stack>
      </div>
    </Box>
  );
};
