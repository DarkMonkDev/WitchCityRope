import React from 'react';
import { Card, Group, Text, Badge, Button, Stack, Checkbox } from '@mantine/core';
import type { GlobalEmailTemplateDto } from '../../services/emailTemplates.api';

/**
 * Event Recipient Group - matches backend enum
 * TODO: Import from auto-generated types when backend DTOs are updated
 */
export type EventRecipientGroup =
  | 'SessionAttendees'
  | 'RSVPTicketHolders'
  | 'SessionVolunteers'
  | 'Teachers';

export interface EnhancedTemplateCardProps {
  template: GlobalEmailTemplateDto & {
    // Extended fields from trigger configuration (will be added to DTO by backend)
    triggerType?: 'FixedEvent' | 'TimeBased' | 'Manual';
    triggerEnabled?: boolean;
    timingOffsetDays?: number;  // +3 = before, -2 = after
    recipientGroup?: EventRecipientGroup;
  };
  onEditTrigger: (templateId: string) => void;
  onEditContent: (templateId: string) => void;
}

/**
 * Helper function to clean variable placeholders from display text
 */
const cleanVariablePlaceholders = (text: string): string => {
  return text
    .replace(/\{\{[^}]+\}\}/g, '{}')
    .replace(/\{[^}]+\}/g, '{}');
};

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
  };

  return labels[group];
};

/**
 * Enhanced Template Card for Events Tab
 *
 * Displays email template with trigger configuration badges and controls.
 * Follows Design System v7 patterns with burgundy/plum color scheme.
 */
export const EnhancedTemplateCard: React.FC<EnhancedTemplateCardProps> = ({
  template,
  onEditTrigger,
  onEditContent,
}) => {
  const {
    id,
    title,
    subject,
    triggerType = 'Manual',
    triggerEnabled = false,
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
        borderColor: 'rgba(136, 1, 36, 0.1)',
        borderRadius: '12px',
        transition: 'all 0.3s ease',
      }}
    >
      <Stack gap="sm">
        {/* Row 1: Title (left) + Enabled checkbox (right) */}
        <Group justify="space-between" wrap="nowrap">
          <Text
            fw={700}
            c="burgundy"
            size="md"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              textTransform: 'uppercase',
              letterSpacing: '1px',
            }}
          >
            {title || 'Untitled Template'}
          </Text>

          <Checkbox
            checked={triggerEnabled}
            readOnly
            label="Enabled"
            size="xs"
            color="burgundy"
            styles={{
              label: { fontSize: '12px', fontWeight: 600, paddingLeft: '6px' },
            }}
          />
        </Group>

        {/* Row 2: Badges — trigger type, timing, recipient */}
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

        {/* Row 3: Subject line */}
        <Text size="sm" c="stone" lineClamp={1}>
          {cleanVariablePlaceholders(subject || '')}
        </Text>

        {/* Row 4: Action buttons (right-aligned, no icons) */}
        <Group justify="flex-end" gap="xs">
          <Button
            variant="light"
            color="burgundy"
            size="xs"
            onClick={() => onEditTrigger(id || '')}
            styles={{
              root: {
                height: '30px',
                fontSize: '12px',
                fontWeight: 600,
              },
            }}
          >
            Edit Trigger
          </Button>

          <Button
            variant="outline"
            color="burgundy"
            size="xs"
            onClick={() => onEditContent(id || '')}
            styles={{
              root: {
                height: '30px',
                fontSize: '12px',
                fontWeight: 600,
              },
            }}
          >
            Edit Email
          </Button>
        </Group>
      </Stack>
    </Card>
  );
};
