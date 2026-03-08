import React, { useState, useEffect } from 'react';
import {
  Modal,
  Stack,
  Group,
  Text,
  Radio,
  NumberInput,
  Select,
  Switch,
  Button,
  Box,
  Divider,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import type { GlobalEmailTemplateDto } from '../../services/emailTemplates.api';
import type { EventRecipientGroup } from './EnhancedTemplateCard';

export interface TriggerConfig {
  triggerType: 'FixedEvent' | 'TimeBased' | 'Manual';
  triggerEnabled: boolean;
  timingOffsetDays?: number;  // +3 = before, -2 = after
  recipientGroup?: EventRecipientGroup;
}

export interface TriggerConfigModalProps {
  opened: boolean;
  onClose: () => void;
  template: GlobalEmailTemplateDto & {
    triggerType?: 'FixedEvent' | 'TimeBased' | 'Manual';
    triggerEnabled?: boolean;
    timingOffsetDays?: number;
    recipientGroup?: EventRecipientGroup;
  };
  onSave: (config: TriggerConfig) => Promise<void>;
}

/**
 * Recipient group options for Events tab
 */
const recipientGroupOptions = [
  {
    value: 'SessionAttendees',
    label: 'Session Attendees',
    description: 'Users who checked in to the session',
  },
  {
    value: 'RSVPTicketHolders',
    label: 'RSVP/Ticket Holders',
    description: 'RSVP for socials OR ticket holders for classes (deduplicated)',
  },
  {
    value: 'SessionVolunteers',
    label: 'Session Volunteers',
    description: 'Volunteers assigned to this specific session',
  },
  {
    value: 'Teachers',
    label: 'Teachers',
    description: 'Teachers assigned to this session',
  },
];

/**
 * Before/After options for timing offset
 */
const beforeAfterOptions = [
  { value: 'before', label: 'Before session start' },
  { value: 'after', label: 'After session start' },
];

/**
 * Trigger Configuration Modal
 *
 * Allows configuration of:
 * - Trigger type (Fixed Event vs Time-Based)
 * - Timing offset (for Time-Based triggers)
 * - Recipient group
 * - Enabled/Disabled state
 */
export const TriggerConfigModal: React.FC<TriggerConfigModalProps> = ({
  opened,
  onClose,
  template,
  onSave,
}) => {
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Form setup with validation
  const form = useForm({
    initialValues: {
      triggerType: (template.triggerType || 'TimeBased') as 'FixedEvent' | 'TimeBased',
      daysOffset: Math.abs(template.timingOffsetDays || 3),
      beforeAfter: (template.timingOffsetDays || 0) >= 0 ? 'before' : 'after',
      recipientGroup: template.recipientGroup || null,
      enabled: template.triggerEnabled !== false, // Default to enabled
    },
    validate: {
      daysOffset: (value, values) =>
        values.triggerType === 'TimeBased' && (!value || value < 1)
          ? 'Days offset must be at least 1'
          : null,
      recipientGroup: (value, values) =>
        values.enabled && !value
          ? 'Recipient group is required when template is enabled'
          : null,
    },
  });

  // Reset form when modal opens with new template
  useEffect(() => {
    if (opened && template) {
      form.setValues({
        triggerType: (template.triggerType || 'TimeBased') as 'FixedEvent' | 'TimeBased',
        daysOffset: Math.abs(template.timingOffsetDays || 3),
        beforeAfter: (template.timingOffsetDays || 0) >= 0 ? 'before' : 'after',
        recipientGroup: template.recipientGroup || null,
        enabled: template.triggerEnabled !== false,
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opened, template]);

  // Handle form submission
  const handleSubmit = async (values: typeof form.values) => {
    setIsSubmitting(true);

    try {
      // Convert before/after + days to single offset number
      const timingOffsetDays =
        values.triggerType === 'TimeBased'
          ? values.beforeAfter === 'before'
            ? values.daysOffset
            : -values.daysOffset
          : undefined;

      const config: TriggerConfig = {
        triggerType: values.triggerType,
        triggerEnabled: values.enabled,
        timingOffsetDays,
        recipientGroup: values.recipientGroup || undefined,
      };

      await onSave(config);
      onClose();
    } catch (error) {
      // Error handling is done in parent component
      console.error('Error saving trigger config:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Configure Email Trigger"
      size="lg"
      centered
      styles={{
        title: {
          fontFamily: "'Montserrat', sans-serif",
          fontWeight: 700,
          fontSize: '20px',
          color: '#880124',
        },
      }}
    >
      <form onSubmit={form.onSubmit(handleSubmit)}>
        <Stack gap="lg">
          {/* Template info */}
          <Box>
            <Text size="sm" fw={600} c="burgundy">
              Template:
            </Text>
            <Text size="md">{template.title || template.templateType}</Text>
            <Text size="sm" c="dimmed">
              {template.subject}
            </Text>
          </Box>

          <Divider />

          {/* Trigger Type */}
          <Box>
            <Text size="sm" fw={600} c="burgundy" mb="xs">
              TRIGGER TYPE
            </Text>
            <Radio.Group
              {...form.getInputProps('triggerType')}
            >
              <Stack gap="sm">
                <Radio
                  value="FixedEvent"
                  label="Fixed Event"
                  description="Triggered by specific actions (purchase, RSVP, etc.)"
                />
                <Radio
                  value="TimeBased"
                  label="Time-Based"
                  description="Triggered relative to session start time"
                />
              </Stack>
            </Radio.Group>
          </Box>

          {/* Timing Offset (only for Time-Based) */}
          {form.values.triggerType === 'TimeBased' && (
            <Box>
              <Text size="sm" fw={600} c="burgundy" mb="xs">
                TIMING OFFSET
              </Text>

              <Group align="flex-end" gap="md">
                <NumberInput
                  label="Send email"
                  {...form.getInputProps('daysOffset')}
                  min={1}
                  max={30}
                  step={1}
                  placeholder="3"
                  style={{ flex: 1 }}
                />

                <Select
                  label="days"
                  data={beforeAfterOptions}
                  {...form.getInputProps('beforeAfter')}
                  style={{ flex: 1 }}
                  searchable={false}
                />

                <Text size="sm" c="dimmed" style={{ marginBottom: '6px' }}>
                  session start
                </Text>
              </Group>

              <Text size="xs" c="dimmed" mt="xs" style={{ fontStyle: 'italic' }}>
                Before = positive days, After = negative days
              </Text>
            </Box>
          )}

          {/* Recipient Group */}
          <Box>
            <Text size="sm" fw={600} c="burgundy" mb="xs">
              RECIPIENT GROUP
            </Text>
            <Text size="xs" c="dimmed" mb="xs">
              Who receives this email?
            </Text>

            <Select
              {...form.getInputProps('recipientGroup')}
              data={recipientGroupOptions}
              placeholder="Select recipient group..."
              searchable={false}
              required={form.values.enabled}
            />
          </Box>

          {/* Enable/Disable */}
          <Box>
            <Text size="sm" fw={600} c="burgundy" mb="xs">
              ENABLE/DISABLE
            </Text>
            <Switch
              {...form.getInputProps('enabled', { type: 'checkbox' })}
              label="Template enabled (will send automatically)"
            />
          </Box>

          <Divider />

          {/* Action Buttons */}
          <Group justify="flex-end" gap="sm">
            <Button
              variant="light"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </Button>

            <Button
              type="submit"
              loading={isSubmitting}
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
              Save Configuration
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
};
