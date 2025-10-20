import React, { useState } from 'react';
import { Card, Title, TextInput, Button, Group, Stack } from '@mantine/core';
import { IconBrandGoogleDrive, IconDeviceFloppy, IconTrash } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { showNotification } from '@mantine/notifications';

interface GoogleDriveLinksSectionProps {
  incidentId: string;
  folderUrl?: string;
  reportUrl?: string;
}

export const GoogleDriveLinksSection: React.FC<GoogleDriveLinksSectionProps> = ({
  incidentId,
  folderUrl: initialFolderUrl,
  reportUrl: initialReportUrl,
}) => {
  const [folderUrl, setFolderUrl] = useState(initialFolderUrl || '');
  const [reportUrl, setReportUrl] = useState(initialReportUrl || '');
  const queryClient = useQueryClient();

  // Update links mutation: PUT /api/safety/admin/incidents/{id}/google-drive
  const updateMutation = useMutation<unknown, Error, void>({
    mutationFn: async () => {
      const response = await fetch(`/api/safety/admin/incidents/${incidentId}/google-drive`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          googleDriveFolderUrl: folderUrl || null,
          googleDriveFinalReportUrl: reportUrl || null,
        }),
      });
      if (!response.ok) throw new Error('Failed to update links');
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'incident', incidentId] });
      showNotification({
        title: 'Success',
        message: 'Google Drive links updated',
        color: 'green',
      });
    },
    onError: (error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : 'Failed to update links',
        color: 'red',
      });
    }
  });

  const handleClear = () => {
    setFolderUrl('');
    setReportUrl('');
  };

  return (
    <Card p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
      <Title order={3} mb="md" style={{ color: '#880124', display: 'flex', alignItems: 'center', gap: '8px' }}>
        <IconBrandGoogleDrive size={20} />
        Google Drive Links
      </Title>

      <Stack gap="md">
        <TextInput
          label="Investigation Folder URL"
          placeholder="https://drive.google.com/drive/folders/..."
          data-testid="google-drive-folder-url"
          value={folderUrl}
          onChange={(e) => setFolderUrl(e.currentTarget.value)}
        />

        <TextInput
          label="Final Report URL"
          placeholder="https://docs.google.com/document/d/..."
          data-testid="google-drive-report-url"
          value={reportUrl}
          onChange={(e) => setReportUrl(e.currentTarget.value)}
        />

        <Group gap="sm">
          <Button
            leftSection={<IconDeviceFloppy size={16} />}
            onClick={() => updateMutation.mutate()}
            loading={updateMutation.isPending}
            data-testid="save-links-button"
            styles={{
              root: {
                fontWeight: 600,
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Save Links
          </Button>
          <Button
            variant="subtle"
            color="red"
            leftSection={<IconTrash size={16} />}
            onClick={handleClear}
            data-testid="clear-links-button"
            styles={{
              root: {
                fontWeight: 600,
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Clear Links
          </Button>
        </Group>
      </Stack>
    </Card>
  );
};
