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
} from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';

interface ReinstateMembershipModalProps {
  opened: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

export const ReinstateMembershipModal: React.FC<ReinstateMembershipModalProps> = ({
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
          Request Membership Reinstatement
        </Title>
      }
      centered
      size="md"
      data-testid="reinstate-membership-modal"
    >
      <Stack gap="md">
        {/* Info Alert */}
        <Alert
          icon={<IconInfoCircle size={20} />}
          title="Admin Review Required"
          color="blue"
          variant="light"
          styles={{
            root: {
              borderLeft: '4px solid #3B82F6',
            },
          }}
        >
          <Text size="sm">
            Your reinstatement request will be reviewed by administrators. Once approved, your full
            vetted membership will be restored.
          </Text>
        </Alert>

        {/* Reason Textarea */}
        <Textarea
          label="Reason (optional)"
          placeholder="e.g., Ready to return, situation resolved, etc."
          value={reason}
          onChange={(e) => setReason(e.currentTarget.value)}
          maxLength={500}
          rows={4}
          data-testid="reinstate-reason-input"
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
            data-testid="reinstate-cancel-button"
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
            color="blue"
            onClick={handleSubmit}
            loading={isSubmitting}
            data-testid="reinstate-confirm-button"
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
            Request Reinstatement
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
