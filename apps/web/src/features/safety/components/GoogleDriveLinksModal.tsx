import React, { useState, useEffect } from 'react';
import {
  Modal,
  Stack,
  Text,
  TextInput,
  Button,
  Group,
  Title,
  Alert
} from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';

interface GoogleDriveLinksModalProps {
  opened: boolean;
  onClose: () => void;
  onSave: (folderUrl?: string, finalReportUrl?: string) => Promise<void>;
  currentFolderUrl?: string;
  currentFinalReportUrl?: string;
}

const GOOGLE_DRIVE_URL_PREFIX = 'https://drive.google.com/';

const validateGoogleDriveUrl = (url: string): boolean => {
  if (!url) return true; // Empty is allowed (optional)
  return url.startsWith(GOOGLE_DRIVE_URL_PREFIX);
};

export const GoogleDriveLinksModal: React.FC<GoogleDriveLinksModalProps> = ({
  opened,
  onClose,
  onSave,
  currentFolderUrl = '',
  currentFinalReportUrl = ''
}) => {
  const [folderUrl, setFolderUrl] = useState(currentFolderUrl);
  const [finalReportUrl, setFinalReportUrl] = useState(currentFinalReportUrl);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [folderUrlError, setFolderUrlError] = useState('');
  const [finalReportUrlError, setFinalReportUrlError] = useState('');

  // Update state when props change
  useEffect(() => {
    if (opened) {
      setFolderUrl(currentFolderUrl || '');
      setFinalReportUrl(currentFinalReportUrl || '');
      setFolderUrlError('');
      setFinalReportUrlError('');
    }
  }, [opened, currentFolderUrl, currentFinalReportUrl]);

  const handleFolderUrlChange = (value: string) => {
    setFolderUrl(value);
    // Trim before validating to handle accidental whitespace
    const trimmedValue = value.trim();
    if (trimmedValue && !validateGoogleDriveUrl(trimmedValue)) {
      setFolderUrlError('URL must start with https://drive.google.com/');
    } else {
      setFolderUrlError('');
    }
  };

  const handleFinalReportUrlChange = (value: string) => {
    setFinalReportUrl(value);
    // Trim before validating to handle accidental whitespace
    const trimmedValue = value.trim();
    if (trimmedValue && !validateGoogleDriveUrl(trimmedValue)) {
      setFinalReportUrlError('URL must start with https://drive.google.com/');
    } else {
      setFinalReportUrlError('');
    }
  };

  const handleSave = async () => {
    // Trim URLs before validation to handle accidental whitespace
    const trimmedFolderUrl = folderUrl.trim();
    const trimmedReportUrl = finalReportUrl.trim();

    // Validate trimmed URLs
    const folderValid = validateGoogleDriveUrl(trimmedFolderUrl);
    const reportValid = validateGoogleDriveUrl(trimmedReportUrl);

    if (!folderValid) {
      setFolderUrlError('URL must start with https://drive.google.com/');
    }
    if (!reportValid) {
      setFinalReportUrlError('URL must start with https://drive.google.com/');
    }

    if (!folderValid || !reportValid) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onSave(
        trimmedFolderUrl || undefined,
        trimmedReportUrl || undefined
      );
      handleClose();
    } catch (error) {
      // Error handled by parent
      console.error('Failed to save Google Drive links:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setFolderUrl(currentFolderUrl || '');
      setFinalReportUrl(currentFinalReportUrl || '');
      setFolderUrlError('');
      setFinalReportUrlError('');
      onClose();
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Update Google Drive Links
        </Title>
      }
      centered
      size="md"
      data-testid="google-drive-links-modal"
    >
      <Stack gap="md">
        {/* Phase 1 Manual Process Alert */}
        <Alert color="blue" icon={<IconInfoCircle />}>
          <Text size="sm">
            <strong>Phase 1: Manual entry only.</strong> Create folders/docs in Google Drive first, then paste links here.
          </Text>
        </Alert>

        {/* Instruction Text */}
        <Text size="sm">
          Enter Google Drive URLs for the investigation folder and final report document.
          Both fields are optional.
        </Text>

        {/* Google Drive Folder URL */}
        <TextInput
          label="Google Drive Folder URL (Optional)"
          placeholder="https://drive.google.com/drive/folders/..."
          value={folderUrl}
          onChange={(e) => handleFolderUrlChange(e.currentTarget.value)}
          error={folderUrlError}
          data-testid="folder-url-input"
          styles={{
            input: {
              fontSize: '16px',
              height: '42px',
              borderRadius: '8px'
            }
          }}
        />

        {/* Google Drive Final Report URL */}
        <TextInput
          label="Google Drive Final Report URL (Optional)"
          placeholder="https://drive.google.com/file/d/..."
          value={finalReportUrl}
          onChange={(e) => handleFinalReportUrlChange(e.currentTarget.value)}
          error={finalReportUrlError}
          data-testid="final-report-url-input"
          styles={{
            input: {
              fontSize: '16px',
              height: '42px',
              borderRadius: '8px'
            }
          }}
        />

        {/* Help Text */}
        <Text size="xs" c="dimmed">
          URLs must start with <strong>https://drive.google.com/</strong>
        </Text>

        {/* Actions */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
            data-testid="google-drive-cancel-button"
            styles={{
              root: {
                minHeight: 40,
                height: 'auto',
                padding: '10px 20px',
                lineHeight: 1.4
              }
            }}
          >
            Cancel
          </Button>
          <Button
            color="purple"
            onClick={handleSave}
            loading={isSubmitting}
            disabled={!!folderUrlError || !!finalReportUrlError}
            data-testid="google-drive-save-button"
            styles={{
              root: {
                minHeight: 40,
                height: 'auto',
                padding: '10px 20px',
                lineHeight: 1.4
              }
            }}
          >
            Save Links
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
