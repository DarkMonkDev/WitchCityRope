import React from 'react';
import { Card, Group, Text, Badge, Stack, Switch } from '@mantine/core';
import type { GlobalEmailTemplateDto } from '../../services/emailTemplates.api';

/**
 * Event Recipient Group - matches backend enum
 * TODO: Import from auto-generated types when backend DTOs are updated
 */
export type EventRecipientGroup =
  | 'SessionAttendees'
  | 'RSVPTicketHolders'
  | 'SessionVolunteers'
  | 'Teachers'
  | 'PendingAssignmentHolders';

export interface EnhancedTemplateCardProps {
  template: GlobalEmailTemplateDto & {
    // Extended fields from trigger configuration (will be added to DTO by backend)
    triggerType?: 'FixedEvent' | 'TimeBased' | 'Manual';
    sendingEnabled?: boolean;
    timingOffsetDays?: number;  // +3 = before, -2 = after
    recipientGroup?: EventRecipientGroup;
  };
  isSelected?: boolean;
  onClick?: () => void;
  onToggleSending?: (templateId: string, enabled: boolean) => void;
}

/**
 * Get recipient group label for display
 */
const getRecipientGroupLabel = (group?: EventRecipientGroup): string => {
  if (!group) return 'No recipient group';

  const labels: Record<EventRecipientGroup, string> = {
    SessionAttendees: 'Session Attendees',
    RSVPTicketHolders: 'RSVP/Ticket Holders',
    SessionVolunteers: 'Session Volunteers',
    Teachers: 'Teachers',
    PendingAssignmentHolders: 'Pending Assignment Holders',
  };

  return labels[group];
};

/**
 * Enhanced Template Card for Events Tab
 *
 * Displays email template with trigger configuration badges and controls.
 * Used by both the global EmailCategoryPanel (Events tab) and the
 * per-event EventEmailTemplatePanel. Click-to-select pattern with
 * inline toggle for sending enabled/disabled.
 *
 * Follows Design System v7 patterns with burgundy/plum color scheme.
 */
export const EnhancedTemplateCard: React.FC<EnhancedTemplateCardProps> = ({
  template,
  isSelected = false,
  onClick,
  onToggleSending,
}) => {
  const {
    id,
    title,
    triggerType = 'Manual',
    sendingEnabled = false,
    timingOffsetDays,
    recipientGroup,
  } = template;

  // Format timing display
  const getTimingDisplay = (): string | null => {
    if (triggerType !== 'TimeBased' || timingOffsetDays === undefined) {
      return null;
    }

    const days = Math.abs(timingOffsetDays);
    const beforeAfter = timingOffsetDays > 0 ? 'before' : 'after';

    return `${days} ${days === 1 ? 'day' : 'days'} ${beforeAfter}`;
  };

  const timingDisplay = getTimingDisplay();

  return (
    <Card
      withBorder
      p="md"
      style={{
        cursor: 'pointer',
        borderColor: isSelected
          ? 'var(--mantine-color-burgundy-6)'
          : 'rgba(136, 1, 36, 0.1)',
        backgroundColor: isSelected ? 'rgba(136, 1, 36, 0.05)' : 'white',
        borderRadius: '12px',
        transition: 'all 0.3s ease',
      }}
      onClick={onClick}
    >
      <Stack gap={4}>
        {/* Row 1: Title (left) + Toggle + Enabled/Disabled badge (right) */}
        <Group justify="space-between" wrap="nowrap">
          <Text fw={600} c="burgundy">
            {title || 'Untitled Template'}
          </Text>

          <Group gap="xs" wrap="nowrap">
            {onToggleSending && (
              <Switch
                size="sm"
                color="green"
                checked={sendingEnabled}
                onChange={(e) => {
                  e.stopPropagation();
                  onToggleSending(id || '', e.currentTarget.checked);
                }}
                onClick={(e) => e.stopPropagation()}
              />
            )}
            <Badge
              size="sm"
              style={{
                backgroundColor: sendingEnabled ? '#2e7d32' : '#c62828',
                color: '#FFF8F0',
                textTransform: 'none',
                fontWeight: 600,
                fontSize: '11px',
              }}
            >
              {sendingEnabled ? 'Enabled' : 'Disabled'}
            </Badge>
          </Group>
        </Group>

        {/* Row 2: Last updated */}
        <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
          Updated{' '}
          {template.updatedAt
            ? new Date(template.updatedAt).toLocaleDateString()
            : 'Never'}
        </Text>

        {/* Row 3: Badges — trigger type, timing, recipient */}
        <Group gap="xs" wrap="wrap">
          <Badge
            variant="filled"
            size="sm"
            style={{
              backgroundColor:
                triggerType === 'TimeBased' ? '#614B79' :
                triggerType === 'FixedEvent' ? '#880124' :
                '#8B8680',
              color: '#FFF8F0',
              textTransform: 'none',
              fontWeight: 600,
              fontSize: '11px',
            }}
          >
            {triggerType === 'TimeBased' ? '⏰ Time-Based' :
             triggerType === 'FixedEvent' ? '⚡ Fixed Event' :
             '✋ Manual'}
          </Badge>

          {triggerType === 'TimeBased' && timingDisplay && (
            <Badge
              size="sm"
              style={{
                backgroundColor: '#B76D75',
                color: '#2B2B2B',
                textTransform: 'none',
                fontWeight: 600,
                fontSize: '11px',
              }}
            >
              📅 {timingDisplay}
            </Badge>
          )}

          {recipientGroup && (
            <Badge
              size="sm"
              style={{
                backgroundColor: '#D4A5A5',
                color: '#2B2B2B',
                textTransform: 'none',
                fontWeight: 600,
                fontSize: '11px',
              }}
            >
              👥 {getRecipientGroupLabel(recipientGroup)}
            </Badge>
          )}
        </Group>
      </Stack>
    </Card>
  );
};
