import React from 'react';
import { Group, Text, Badge } from '@mantine/core';
import { IconNotes } from '@tabler/icons-react';
import type { components } from '@witchcityrope/shared-types';
import { VettingStatusBadge } from './VettingStatusBadge';

type ApplicationNoteDto = components['schemas']['ApplicationNoteDto'];

// Helper to detect system-generated notes and extract status
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

// Format time helper function
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

export const VettingNoteRenderer = (note: ApplicationNoteDto): React.ReactNode => {
  // Check if this is a system-generated status change note
  const { isSystem, status } = isSystemGeneratedNote(note.content || '');

  return (
    <>
      <Group justify="space-between" mb="xs">
        <Group gap="xs">
          {/* Show status badge for system-generated notes */}
          {isSystem && status ? (
            <VettingStatusBadge status={status} size="sm" />
          ) : (
            <IconNotes size={16} style={{ color: '#880124' }} />
          )}
          <Text fw={600} size="sm">
            {note.reviewerName}
          </Text>
        </Group>
        <Text size="sm" c="dimmed">
          {formatTime(note.createdAt || '')}
        </Text>
      </Group>
      {note.tags && note.tags.length > 0 && (
        <Group gap="xs" mt="xs">
          {note.tags.map((tag, idx) => (
            <Badge key={idx} size="sm" variant="light" color="gray">
              {tag}
            </Badge>
          ))}
        </Group>
      )}
    </>
  );
};
