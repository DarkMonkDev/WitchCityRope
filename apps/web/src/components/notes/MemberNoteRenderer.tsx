import React from 'react';
import { components } from '@witchcityrope/shared-types';
import { Group, Text, Paper } from '@mantine/core';
import { IconNotes } from '@tabler/icons-react';
import { VettingStatusBadge } from '@/features/admin/vetting/components/VettingStatusBadge';

type MemberNoteHistoryResponse = components['schemas']['MemberNoteHistoryResponse'];

// Helper to detect system-generated notes and extract status
// EXACT same logic as VettingNoteRenderer
const isSystemGeneratedNote = (noteText: string): { isSystem: boolean; status?: string } => {
  // Map system-generated note text to corresponding status values
  // These match the simplified descriptions from backend GetSimplifiedActionDescription()
  const systemNotes: Record<string, string> = {
    'Approved for interview': 'InterviewApproved',
    'Interview completed': 'FinalReview',
    'Application approved': 'Approved',
    'Application denied': 'Denied',
    'Application placed on hold': 'OnHold',
    'Returned to review': 'UnderReview',
    'Application withdrawn': 'Withdrawn',
  };

  const status = systemNotes[noteText];
  return { isSystem: !!status, status };
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
      {/* Display content as-is (backend provides simplified descriptions) */}
      <Text size="sm">{note.content}</Text>
    </Paper>
  );
};
