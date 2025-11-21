import React, { useState } from 'react';
import { Paper, Stack, Title, Text, Badge, Group, Button, Modal, Alert } from '@mantine/core';
import { IconCheck, IconClock, IconX, IconAlertCircle } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import type { VolunteerPosition } from '../../features/volunteers/types/volunteer.types';
import { cancelVolunteerSignup } from '../../features/volunteers/api/volunteerApi';

interface UserVolunteerShiftsProps {
  positions: VolunteerPosition[];
  eventId: string;
}

/**
 * User Volunteer Shifts Component
 * Displays the shifts the user has signed up for on an event
 * Shows only when user has already volunteered for this event
 * Allows user to cancel volunteer signup (backend validates timing)
 */
export const UserVolunteerShifts: React.FC<UserVolunteerShiftsProps> = ({
  positions,
  eventId
}) => {
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [selectedPosition, setSelectedPosition] = useState<VolunteerPosition | null>(null);
  const queryClient = useQueryClient();

  // Cancel volunteer signup mutation
  const cancelMutation = useMutation({
    mutationFn: (assignmentId: string) => cancelVolunteerSignup(assignmentId),
    onSuccess: () => {
      notifications.show({
        title: 'Success',
        message: 'Your volunteer shift has been cancelled.',
        color: 'green',
        icon: <IconCheck size={16} />,
      });
      // Refetch event volunteer positions
      queryClient.invalidateQueries({ queryKey: ['events', eventId, 'volunteer-positions'] });
      queryClient.invalidateQueries({ queryKey: ['user', 'volunteer-shifts'] });
      setCancelModalOpen(false);
      setSelectedPosition(null);
    },
    onError: (error: any) => {
      notifications.show({
        title: 'Error',
        message: error.response?.data?.message || 'Failed to cancel volunteer shift. Please try again.',
        color: 'red',
        icon: <IconX size={16} />,
      });
    },
  });

  const handleCancelClick = (position: VolunteerPosition) => {
    setSelectedPosition(position);
    setCancelModalOpen(true);
  };

  const handleCancelConfirm = () => {
    if (selectedPosition?.userSignupId) {
      cancelMutation.mutate(selectedPosition.userSignupId);
    }
  };

  const formatTime = (timeString?: string) => {
    if (!timeString) return '';
    try {
      const date = new Date(timeString);
      const formatted = date.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      });
      return formatted.replace(/AM|PM/g, match => match.toLowerCase());
    } catch {
      return timeString;
    }
  };

  return (
    <Paper
      style={{
        background: 'var(--color-ivory)',
        borderRadius: '16px',
        padding: 'var(--space-lg)',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
        border: '1px solid rgba(34, 139, 34, 0.2)'
      }}
    >
      <Stack gap="md">
        {/* Header with success badge */}
        <Group gap="sm" align="center">
          <div
            style={{
              width: '36px',
              height: '36px',
              borderRadius: '50%',
              background: 'var(--color-success-light, rgba(34, 139, 34, 0.1))',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}
          >
            <IconCheck size={20} color="var(--color-success, #228B22)" />
          </div>
          <Title
            order={3}
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '18px',
              fontWeight: 700,
              color: 'var(--color-success, #228B22)'
            }}
          >
            You're Volunteering!
          </Title>
        </Group>

        {/* List of positions */}
        <Stack gap="xs">
          {positions.map((position) => (
            <div
              key={position.id}
              style={{
                background: 'white',
                borderRadius: '8px',
                padding: 'var(--space-sm)',
                border: '1px solid var(--color-stone-light)'
              }}
            >
              <Group justify="space-between" align="flex-start" wrap="nowrap">
                <Stack gap={4} style={{ flex: 1 }}>
                  <Group gap="xs" wrap="wrap">
                    <Text
                      size="sm"
                      fw={600}
                      style={{ color: 'var(--color-charcoal)' }}
                    >
                      {position.title}
                    </Text>
                    {/* Only show session name if event has multiple sessions (hide "Main Session" for single-session events) */}
                    {position.sessionName && !position.sessionName.includes('Main Session') && (
                      <Badge size="xs" color="gray" variant="light">
                        {position.sessionName}
                      </Badge>
                    )}
                  </Group>

                  {position.sessionStartTime && position.sessionEndTime && (
                    <Group gap="xs">
                      <IconClock size={12} color="var(--color-stone)" />
                      <Text size="xs" c="dimmed">
                        {formatTime(position.sessionStartTime)} - {formatTime(position.sessionEndTime)}
                      </Text>
                    </Group>
                  )}
                </Stack>

                {/* Cancel Button or Message - Based on timing permissions from backend */}
                {position.hasUserSignedUp && position.userSignupId && (
                  <>
                    {position.canCancel ? (
                      <Button
                        variant="subtle"
                        color="red"
                        size="xs"
                        onClick={() => handleCancelClick(position)}
                        disabled={cancelMutation.isPending}
                        style={{ minWidth: '80px' }}
                      >
                        Cancel
                      </Button>
                    ) : (
                      <Text size="xs" c="dimmed" style={{ fontSize: '11px', textAlign: 'right' }}>
                        Cancellation window closed
                      </Text>
                    )}
                  </>
                )}
              </Group>
            </div>
          ))}
        </Stack>

        {/* Thank you message */}
        <Text
          size="sm"
          c="dimmed"
          ta="center"
          style={{ paddingTop: 'var(--space-xs)' }}
        >
          Thank you for helping make this event possible!
        </Text>
      </Stack>

      {/* Cancellation Confirmation Modal */}
      <Modal
        opened={cancelModalOpen}
        onClose={() => {
          setCancelModalOpen(false);
          setSelectedPosition(null);
        }}
        title={<Title order={3}>Cancel Volunteer Shift?</Title>}
        centered
      >
        <Stack gap="md">
          <Text>
            Are you sure you want to cancel your volunteer shift for{' '}
            <strong>{selectedPosition?.title}</strong>?
          </Text>

          {selectedPosition?.sessionName && !selectedPosition.sessionName.includes('Main Session') && (
            <Text size="sm" c="dimmed">
              Session: {selectedPosition.sessionName}
            </Text>
          )}

          <Text size="sm" c="dimmed">
            This action cannot be undone. The position will become available for other volunteers.
          </Text>

          <Group justify="flex-end" mt="md">
            <Button
              variant="default"
              onClick={() => {
                setCancelModalOpen(false);
                setSelectedPosition(null);
              }}
              disabled={cancelMutation.isPending}
            >
              Keep My Shift
            </Button>
            <Button
              color="red"
              onClick={handleCancelConfirm}
              loading={cancelMutation.isPending}
            >
              Cancel Shift
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Paper>
  );
};
