import React from 'react';
import { components } from '@witchcityrope/shared-types';
import { Group, Text, Paper } from '@mantine/core';
import { IconNotes } from '@tabler/icons-react';
import { VettingStatusBadge } from '@/features/admin/vetting/components/VettingStatusBadge';
import { detectSystemGeneratedNote } from '@/features/vetting/utils/vettingAuditHelpers';

type MemberNoteHistoryResponse = components['schemas']['MemberNoteHistoryResponse'];

// Format time helper function - EXACT same as VettingNoteRenderer
const formatTime = (dateString: string) => {
  const date = new Date(dateString);
  const dateStr = date.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
  const timeStr = date.toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
  return `${dateStr} - ${timeStr}`;
};

export const MemberNoteRenderer = (note: MemberNoteHistoryResponse): React.ReactNode => {
  // Check if this is a system-generated status change note. See
  // features/vetting/utils/vettingAuditHelpers for the prefix → status
  // mapping that's now shared with VettingNoteRenderer.
  const { isSystem, status } = detectSystemGeneratedNote(note.content);

  return (
    <Paper key={note.id} p="md" style={{ background: '#F5F5F5' }}>
      <Group justify="space-between" mb="xs">
        <Group gap="xs">
          {/* Show status badge for system-generated notes */}
          {isSystem && status ? (
            <VettingStatusBadge status={status} size="sm" />
          ) : (
            <IconNotes size={16} style={{ color: '#880124' }} />
          )}
          <Text fw={600} size="sm">
            {note.authorSceneName || 'System'}
          </Text>
        </Group>
        <Text size="sm" c="dimmed">
          {formatTime(note.timestamp || '')}
        </Text>
      </Group>
      {/* Display content with preserved line breaks for admin reasons */}
      <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{note.content}</Text>
    </Paper>
  );
};
