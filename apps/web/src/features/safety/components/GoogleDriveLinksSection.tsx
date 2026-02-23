import React, { useState, useEffect } from 'react';
import { Card, Title, TextInput, Button, Group, Stack, Text, Anchor, Grid } from '@mantine/core';
import { IconBrandGoogleDrive, IconDeviceFloppy } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { showNotification } from '@mantine/notifications';
import { apiClient } from '../../../lib/api/client';

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
  const [editingFolder, setEditingFolder] = useState(!initialFolderUrl);
  const [editingReport, setEditingReport] = useState(!initialReportUrl);
  const queryClient = useQueryClient();

  // Update local state when props change
  useEffect(() => {
    setFolderUrl(initialFolderUrl || '');
    setReportUrl(initialReportUrl || '');
    setEditingFolder(!initialFolderUrl);
    setEditingReport(!initialReportUrl);
  }, [initialFolderUrl, initialReportUrl]);

  // Check if there are unsaved changes
  const hasChanges = folderUrl !== (initialFolderUrl || '') || reportUrl !== (initialReportUrl || '');

  // Update links mutation: PUT /api/safety/admin/incidents/{id}/google-drive
  const updateMutation = useMutation<unknown, Error, void>({
    mutationFn: async () => {
      const response = await apiClient.put(`/api/safety/admin/incidents/${incidentId}/google-drive`, {
        googleDriveFolderUrl: folderUrl || null,
        googleDriveFinalReportUrl: reportUrl || null,
      });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'incident', incidentId] });
      setEditingFolder(false);
      setEditingReport(false);
      showNotification({
        title: 'Success',
        message: 'Google Drive links updated',
        color: 'green',
      });
    },
    onError: (error: Error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : 'Failed to update links',
        color: 'red',
      });
    }
  });

  return (
    <Card p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
      <Group justify="space-between" align="center" mb="md">
        <Title order={3} style={{ color: '#880124', display: 'flex', alignItems: 'center', gap: '8px' }}>
          <IconBrandGoogleDrive size={20} />
          Google Drive Links
        </Title>
        <Button
          leftSection={<IconDeviceFloppy size={16} />}
          onClick={() => updateMutation.mutate()}
          loading={updateMutation.isPending}
          disabled={!hasChanges}
          data-testid="save-links-button"
        >
          Save Links
        </Button>
      </Group>

      <Stack gap="md">
        <Grid gutter="xl">
          {/* Investigation Folder */}
          <Grid.Col span={6}>
            {editingFolder && <Text size="sm" c="dimmed" mb="xs">Investigation Folder</Text>}
            {editingFolder ? (
              <TextInput
                placeholder="https://drive.google.com/drive/folders/..."
                data-testid="google-drive-folder-url"
                value={folderUrl}
                onChange={(e) => setFolderUrl(e.currentTarget.value)}
              />
            ) : (
              <Group gap="lg">
                <Anchor
                  href={folderUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{ fontSize: '1.125rem', fontWeight: 500 }}
                >
                  Investigation Folder
                </Anchor>
                <Anchor
                  size="sm"
                  onClick={() => setEditingFolder(true)}
                  style={{ cursor: 'pointer' }}
                >
                  edit
                </Anchor>
              </Group>
            )}
          </Grid.Col>

          {/* Final Report */}
          <Grid.Col span={6}>
            {editingReport && <Text size="sm" c="dimmed" mb="xs">Final Report</Text>}
            {editingReport ? (
              <TextInput
                placeholder="https://docs.google.com/document/d/..."
                data-testid="google-drive-report-url"
                value={reportUrl}
                onChange={(e) => setReportUrl(e.currentTarget.value)}
              />
            ) : (
              <Group gap="lg">
                <Anchor
                  href={reportUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{ fontSize: '1.125rem', fontWeight: 500 }}
                >
                  Final Report
                </Anchor>
                <Anchor
                  size="sm"
                  onClick={() => setEditingReport(true)}
                  style={{ cursor: 'pointer' }}
                >
                  edit
                </Anchor>
              </Group>
            )}
          </Grid.Col>
        </Grid>
      </Stack>
    </Card>
  );
};
