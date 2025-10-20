import { Modal, Stack, Text, Group, Badge, Button, Textarea, Alert, Progress, Divider } from '@mantine/core';
import { IconUsers, IconClock, IconAlertCircle, IconCheck, IconInfoCircle } from '@tabler/icons-react';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { signupForVolunteerPosition } from '../api/volunteerApi';
import type { VolunteerPosition } from '../types/volunteer.types';
import { useCurrentUser } from '@/lib/api/hooks/useAuth';
import { notifications } from '@mantine/notifications';

interface VolunteerPositionModalProps {
  position: VolunteerPosition | null;
  opened: boolean;
  onClose: () => void;
}

export const VolunteerPositionModal: React.FC<VolunteerPositionModalProps> = ({
  position,
  opened,
  onClose
}) => {
  const [notes, setNotes] = useState('');
  const { data: currentUser } = useCurrentUser();
  const queryClient = useQueryClient();

  const signupMutation = useMutation({
    mutationFn: async () => {
      if (!position) throw new Error('No position selected');
      return signupForVolunteerPosition(position.id, { notes: notes.trim() || undefined });
    },
    onSuccess: (response) => {
      notifications.show({
        title: 'Success!',
        message: response.message || 'You have been signed up for this volunteer position. You have also been automatically RSVPed to the event.',
        color: 'green',
        icon: <IconCheck size={16} />
      });

      // Invalidate queries to refresh the volunteer positions list
      queryClient.invalidateQueries({ queryKey: ['volunteerPositions', position?.eventId] });

      setNotes('');
      onClose();
    },
    onError: (error: any) => {
      notifications.show({
        title: 'Signup Failed',
        message: error.response?.data?.error || error.message || 'Failed to sign up for volunteer position',
        color: 'red',
        icon: <IconAlertCircle size={16} />
      });
    }
  });

  if (!position) return null;

  const filledPercentage = (position.slotsFilled / position.slotsNeeded) * 100;
  const isAuthenticated = !!currentUser;

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={
        <Text size="xl" fw={700} style={{ color: 'var(--color-text)' }}>
          Volunteer Position
        </Text>
      }
      size="lg"
      centered
    >
      <Stack gap="lg">
        {/* Header */}
        <div>
          <Group justify="space-between" mb="xs">
            <Text size="xl" fw={700} style={{ color: 'var(--color-burgundy)' }}>
              {position.title}
            </Text>
            {position.hasUserSignedUp && (
              <Badge color="green" variant="filled" size="lg">
                Already Signed Up
              </Badge>
            )}
          </Group>

          {position.sessionName && (
            <Group gap="xs">
              <IconClock size={16} style={{ color: 'var(--color-stone)' }} />
              <Text size="sm" c="dimmed">
                {position.sessionName}
              </Text>
            </Group>
          )}
        </div>

        {/* Description */}
        <div>
          <Text size="sm" fw={600} mb="xs">
            Description
          </Text>
          <Text size="sm" c="dimmed">
            {position.description}
          </Text>
        </div>

        {/* Requirements */}
        {position.requirements && (
          <div>
            <Text size="sm" fw={600} mb="xs">
              Requirements
            </Text>
            <Text size="sm" c="dimmed">
              {position.requirements}
            </Text>
            {position.requiresExperience && (
              <Alert
                icon={<IconAlertCircle size={16} />}
                color="yellow"
                variant="light"
                mt="xs"
              >
                This position requires previous experience
              </Alert>
            )}
          </div>
        )}

        <Divider />

        {/* Availability */}
        <div>
          <Group justify="space-between" mb="xs">
            <Group gap={6}>
              <IconUsers size={16} style={{ color: 'var(--color-stone)' }} />
              <Text size="sm" fw={600}>
                Volunteer Spots
              </Text>
            </Group>
            <Text size="sm" c="dimmed">
              {position.slotsFilled} / {position.slotsNeeded} filled
            </Text>
          </Group>
          <Progress
            value={filledPercentage}
            color={position.isFullyStaffed ? 'gray' : 'blue'}
            size="md"
            radius="xl"
          />
          {position.slotsRemaining > 0 && position.slotsRemaining <= 2 && (
            <Text size="xs" c="orange" mt="xs">
              Only {position.slotsRemaining} spot{position.slotsRemaining === 1 ? '' : 's'} remaining!
            </Text>
          )}
        </div>

        {/* Auto-RSVP Notice */}
        {isAuthenticated && !position.hasUserSignedUp && !position.isFullyStaffed && (
          <Alert
            icon={<IconInfoCircle size={16} />}
            color="blue"
            variant="light"
          >
            Signing up for this volunteer position will automatically RSVP you to the event if you haven't already.
          </Alert>
        )}

        {/* Signup Form */}
        {isAuthenticated && !position.hasUserSignedUp && !position.isFullyStaffed ? (
          <>
            <Textarea
              label="Notes (Optional)"
              placeholder="Any questions or special considerations?"
              value={notes}
              onChange={(e) => setNotes(e.currentTarget.value)}
              minRows={3}
              maxRows={6}
            />

            <Group justify="space-between">
              <Button variant="subtle" onClick={onClose}>
                Cancel
              </Button>
              <Button
                onClick={() => signupMutation.mutate()}
                loading={signupMutation.isPending}
                color="blue"
              >
                Sign Up
              </Button>
            </Group>
          </>
        ) : position.isFullyStaffed ? (
          <Alert color="gray" variant="light">
            This volunteer position is currently full
          </Alert>
        ) : position.hasUserSignedUp ? (
          <Alert color="green" variant="light" icon={<IconCheck size={16} />}>
            You're already signed up for this position
          </Alert>
        ) : (
          <Alert color="blue" variant="light">
            Please log in to sign up for this volunteer position
          </Alert>
        )}
      </Stack>
    </Modal>
  );
};
