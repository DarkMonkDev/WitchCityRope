import React, { useState, useEffect } from 'react';
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
  Textarea,
  Alert,
  List,
} from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';

interface HoldMembershipModalProps {
  opened: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

export const HoldMembershipModal: React.FC<HoldMembershipModalProps> = ({
  opened,
  onClose,
  onConfirm,
}) => {
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset state when modal closes
  useEffect(() => {
    if (!opened) {
      setReason('');
    }
  }, [opened]);

  const handleSubmit = async () => {
    setIsSubmitting(true);
    try {
      await onConfirm(reason);
      setReason('');
      onClose();
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setReason('');
      onClose();
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Put Membership on Hold
        </Title>
      }
      centered
      size="md"
      data-testid="hold-membership-modal"
    >
      <Stack gap="md">
        {/* Warning Alert */}
        <Alert
          icon={<IconAlertTriangle size={20} />}
          title="This action will:"
          color="yellow"
          variant="light"
          styles={{
            root: {
              borderLeft: '4px solid #DAA520',
            },
          }}
        >
          <List size="sm" spacing="xs" withPadding={false}>
            <List.Item>Remove you from our mailing list</List.Item>
            <List.Item>Prevent you from attending or RSVPing to social events</List.Item>
            <List.Item>Require admin approval to reinstate your membership</List.Item>
          </List>
        </Alert>

        {/* Reassuring Text */}
        <Text size="sm" c="dimmed">
          You can reinstate your membership at a later time.
        </Text>

        {/* Reason Textarea */}
        <Textarea
          label="Reason to put membership on hold (optional)"
          placeholder="e.g., Taking a break, personal reasons, etc."
          value={reason}
          onChange={(e) => setReason(e.currentTarget.value)}
          maxLength={500}
          rows={4}
          data-testid="hold-reason-input"
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <Text size="xs" c="dimmed" ta="right">
          {reason.length}/500 characters
        </Text>

        {/* Action Buttons */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
            data-testid="hold-cancel-button"
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
            Cancel
          </Button>
          <Button
            color="yellow"
            onClick={handleSubmit}
            loading={isSubmitting}
            data-testid="hold-confirm-button"
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
            Yes, Put on Hold
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
