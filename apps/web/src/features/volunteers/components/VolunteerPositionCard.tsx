import { Paper, Group, Text, Badge, Button, Stack, Progress } from '@mantine/core';
import { IconClock, IconUsers, IconAlertCircle } from '@tabler/icons-react';
import type { VolunteerPosition } from '../types/volunteer.types';

interface VolunteerPositionCardProps {
  position: VolunteerPosition;
  onLearnMore: () => void;
}

export const VolunteerPositionCard: React.FC<VolunteerPositionCardProps> = ({
  position,
  onLearnMore
}) => {
  const filledPercentage = (position.slotsFilled / position.slotsNeeded) * 100;

  return (
    <Paper
      style={{
        background: 'white',
        border: '1px solid var(--color-stone-light)',
        borderRadius: '16px',
        padding: 'var(--space-lg)',
        cursor: 'pointer',
        transition: 'all 0.3s ease'
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.08)';
        e.currentTarget.style.transform = 'translateY(-2px)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.boxShadow = 'none';
        e.currentTarget.style.transform = 'translateY(0)';
      }}
      onClick={onLearnMore}
    >
      <Stack gap="md">
        {/* Header */}
        <Group justify="space-between" align="flex-start">
          <div>
            <Group gap="xs" mb={4}>
              <Text
                size="lg"
                fw={700}
                style={{ color: 'var(--color-text)' }}
              >
                {position.title}
              </Text>
              {position.hasUserSignedUp && (
                <Badge
                  color="green"
                  variant="light"
                  size="sm"
                >
                  Signed Up
                </Badge>
              )}
            </Group>

            {position.sessionName && (
              <Group gap="xs">
                <IconClock size={14} style={{ color: 'var(--color-stone)' }} />
                <Text size="sm" c="dimmed">
                  {position.sessionName}
                </Text>
              </Group>
            )}
          </div>

          {position.isFullyStaffed ? (
            <Badge color="gray" variant="filled" size="lg">
              Full
            </Badge>
          ) : position.slotsRemaining <= 2 ? (
            <Badge color="orange" variant="light" size="lg">
              {position.slotsRemaining} left
            </Badge>
          ) : (
            <Badge color="blue" variant="light" size="lg">
              {position.slotsRemaining} spots
            </Badge>
          )}
        </Group>

        {/* Description Preview */}
        <Text size="sm" c="dimmed" lineClamp={2}>
          {position.description}
        </Text>

        {/* Slots Progress */}
        <div>
          <Group justify="space-between" mb={4}>
            <Group gap={6}>
              <IconUsers size={14} style={{ color: 'var(--color-stone)' }} />
              <Text size="xs" c="dimmed">
                {position.slotsFilled} / {position.slotsNeeded} volunteers
              </Text>
            </Group>
          </Group>
          <Progress
            value={filledPercentage}
            color={position.isFullyStaffed ? 'gray' : 'blue'}
            size="sm"
            radius="xl"
          />
        </div>

        {/* Requirements Badge */}
        {position.requiresExperience && (
          <Group gap={6}>
            <IconAlertCircle size={14} style={{ color: 'var(--color-warning)' }} />
            <Text size="xs" style={{ color: 'var(--color-warning)' }}>
              Experience required
            </Text>
          </Group>
        )}

        {/* Learn More Button */}
        <Button
          variant="light"
          color="blue"
          fullWidth
          size="sm"
          onClick={(e) => {
            e.stopPropagation();
            onLearnMore();
          }}
        >
          Learn More & Sign Up
        </Button>
      </Stack>
    </Paper>
  );
};
