import React from 'react';
import { components } from '@witchcityrope/shared-types';
import { Group, Text, Paper } from '@mantine/core';
import { IconNotes } from '@tabler/icons-react';
import { VettingStatusBadge } from '@/features/admin/vetting/components/VettingStatusBadge';

type MemberNoteHistoryResponse = components['schemas']['MemberNoteHistoryResponse'];

// Helper to detect system-generated notes and extract status
// Uses startsWith matching to support notes with appended admin reasons
const isSystemGeneratedNote = (noteText: string): { isSystem: boolean; status?: string } => {
  // Map system-generated note prefixes to corresponding status values
  // These match the simplified descriptions from backend GetSimplifiedActionDescription()
  // Notes may have admin reasons appended after "\n\nReason: ..."
  const systemNotePrefixes: Array<{ prefix: string; status: string }> = [
    { prefix: 'Approved for interview', status: 'InterviewApproved' },
    { prefix: 'Interview completed', status: 'FinalReview' },
    { prefix: 'Application approved', status: 'Approved' },
    { prefix: 'Application denied', status: 'Denied' },
    { prefix: 'Application placed on hold', status: 'OnHold' },
    { prefix: 'Returned to review', status: 'UnderReview' },
    { prefix: 'Application withdrawn', status: 'Withdrawn' },
  ];

  // Check if noteText starts with any known prefix
  for (const { prefix, status } of systemNotePrefixes) {
    if (noteText.startsWith(prefix)) {
      return { isSystem: true, status };
    }
  }

  return { isSystem: false };
};

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
  // Check if this is a system-generated status change note
  const { isSystem, status } = isSystemGeneratedNote(note.content || '');

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
          {formatTime(note.timestamp)}
        </Text>
      </Group>
      {/* Display content with preserved line breaks for admin reasons */}
      <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{note.content}</Text>
    </Paper>
  );
};
