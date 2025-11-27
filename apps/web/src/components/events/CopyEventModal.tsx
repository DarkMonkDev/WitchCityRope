import React, { useEffect } from 'react';
import { Modal, Stack, TextInput, Button, Group, Title } from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { useNavigate } from 'react-router-dom';
import { useCopyEvent } from '../../features/events/api/mutations';
import { StyledDatePicker } from '../forms/StyledDatePicker';

interface CopyEventModalProps {
  opened: boolean;
  onClose: () => void;
  eventToCopy: { id: string; title: string; startDate: string } | null;
}

export const CopyEventModal: React.FC<CopyEventModalProps> = ({
  opened,
  onClose,
  eventToCopy,
}) => {
  const navigate = useNavigate();
  const copyEventMutation = useCopyEvent();

  // Form setup with validation
  const form = useForm({
    initialValues: {
      newDate: new Date(),
      newTitle: '',
    },
    validate: {
      newDate: (value) => {
        if (!value) return 'Date is required';
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        if (value < today) return 'Event date cannot be in the past';
        return null;
      },
      newTitle: (value) => {
        if (!value || value.trim().length === 0) return 'Title is required';
        if (value.length < 3) return 'Title must be at least 3 characters';
        if (value.length > 200) return 'Title cannot exceed 200 characters';
        return null;
      },
    },
  });

  // Update form values when modal opens with new event
  useEffect(() => {
    if (opened && eventToCopy) {
      form.setValues({
        newDate: eventToCopy.startDate
          ? new Date(new Date(eventToCopy.startDate).getTime() + 7 * 24 * 60 * 60 * 1000) // Default to 7 days after original
          : new Date(),
        newTitle: `${eventToCopy.title} (Copy)`,
      });
    }
  }, [opened, eventToCopy]);

  // Reset form when modal closes
  useEffect(() => {
    if (!opened) {
      form.reset();
    }
  }, [opened]);

  // Submit handler
  const handleSubmit = form.onSubmit(async (values) => {
    if (!eventToCopy) return;

    try {
      const copiedEvent = await copyEventMutation.mutateAsync({
        eventId: eventToCopy.id,
        newStartDate: values.newDate.toISOString(),
        newTitle: values.newTitle.trim(),
      });

      notifications.show({
        title: 'Success',
        message: 'Event copied successfully. Redirecting to edit page.',
        color: 'green',
      });

      // Close modal first
      onClose();

      // Navigate to edit page for the copied event
      const copiedEventId = (copiedEvent as any)?.id;
      if (copiedEventId) {
        navigate(`/admin/events/${copiedEventId}`);
      }
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || error?.message || 'Unable to copy event. Please try again.';
      notifications.show({
        title: 'Error',
        message: errorMessage,
        color: 'red',
      });
    }
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={<Title order={3} style={{ color: '#880124' }}>Copy Event</Title>}
      centered
      size="md"
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <StyledDatePicker
            label="New Event Date"
            placeholder="Select date"
            required
            minDate={new Date()}
            data-testid="input-event-date"
            {...form.getInputProps('newDate')}
          />

          <TextInput
            label="New Event Title"
            placeholder="e.g., Spring Workshop 2025"
            required
            maxLength={200}
            data-testid="input-event-title"
            {...form.getInputProps('newTitle')}
          />

          <Group justify="flex-end" gap="md" mt="md">
            <Button
              variant="outline"
              onClick={onClose}
              disabled={copyEventMutation.isPending}
            >
              Cancel
            </Button>

            <Button
              type="submit"
              loading={copyEventMutation.isPending}
              styles={{
                root: {
                  fontWeight: 600,
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                },
              }}
            >
              Copy Event
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
};
