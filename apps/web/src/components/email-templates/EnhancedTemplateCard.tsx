import React from 'react';
import { Card, Group, Text, Badge, Button, Stack, Box } from '@mantine/core';
import { IconEdit, IconSettings } from '@tabler/icons-react';
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
    templateTypeName,
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
      <Stack gap="md">
        {/* Header with title and action buttons */}
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
            {templateTypeName}
          </Text>

          <Group gap="xs" wrap="nowrap">
            <Button
              variant="light"
              color="burgundy"
              size="xs"
              onClick={() => onEditTrigger(id)}
              leftSection={<IconSettings size={14} />}
              styles={{
                root: {
                  height: '32px',
                  paddingTop: '6px',
                  paddingBottom: '6px',
                  fontSize: '12px',
                  lineHeight: '1.2',
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
              onClick={() => onEditContent(id)}
              leftSection={<IconEdit size={14} />}
              styles={{
                root: {
                  height: '32px',
                  paddingTop: '6px',
                  paddingBottom: '6px',
                  fontSize: '12px',
                  lineHeight: '1.2',
                  fontWeight: 600,
                },
              }}
            >
              Edit
            </Button>
          </Group>
        </Group>

        {/* Subject line */}
        <Text size="sm" c="stone" lineClamp={1}>
          {cleanVariablePlaceholders(subject)}
        </Text>

        {/* Badge row - Trigger type, timing, recipient */}
        <Group gap="xs" wrap="wrap">
          {/* Trigger type badge */}
          <Badge
            color={triggerType === 'TimeBased' ? 'grape' : triggerType === 'FixedEvent' ? 'red' : 'gray'}
            variant="filled"
            size="sm"
            style={{
              backgroundColor:
                triggerType === 'TimeBased' ? '#614B79' : // Plum
                triggerType === 'FixedEvent' ? '#880124' : // Burgundy
                '#8B8680', // Stone gray for Manual
              color: '#FFF8F0', // Ivory text
              textTransform: 'none',
              fontWeight: 600,
              fontSize: '11px',
            }}
          >
            {triggerType === 'TimeBased' ? '⏰ Time-Based' :
             triggerType === 'FixedEvent' ? '⚡ Fixed Event' :
             '✋ Manual'}
          </Badge>

          {/* Timing badge (only for time-based) */}
          {triggerType === 'TimeBased' && timingDisplay && (
            <Badge
              size="sm"
              style={{
                backgroundColor: '#B76D75', // Rose gold
                color: '#2B2B2B', // Charcoal text
                textTransform: 'none',
                fontWeight: 600,
                fontSize: '11px',
              }}
            >
              📅 {timingDisplay}
            </Badge>
          )}

          {/* Recipient badge */}
          {recipientGroup && (
            <Badge
              size="sm"
              style={{
                backgroundColor: '#D4A5A5', // Dusty rose
                color: '#2B2B2B', // Charcoal text
                textTransform: 'none',
                fontWeight: 600,
                fontSize: '11px',
              }}
            >
              👥 {getRecipientGroupLabel(recipientGroup)}
            </Badge>
          )}
        </Group>

        {/* Status indicator */}
        <Box>
          <Text size="xs" c={triggerEnabled ? 'green' : 'dimmed'} fw={600}>
            {triggerEnabled ? '✅ Enabled' : '❌ Disabled'}
            {triggerType === 'FixedEvent' && triggerEnabled && (
              <Text component="span" c="dimmed" ml="xs">
                (triggers on action)
              </Text>
            )}
          </Text>
        </Box>
      </Stack>
    </Card>
  );
};
