import React from 'react';
import { Group, Text, Badge } from '@mantine/core';
import { IconNotes } from '@tabler/icons-react';
import type { components } from '@witchcityrope/shared-types';
import { VettingStatusBadge } from './VettingStatusBadge';
import { detectSystemGeneratedNote } from '../../../vetting/utils/vettingAuditHelpers';

type ApplicationNoteDto = components['schemas']['ApplicationNoteDto'];

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
  // Check if this is a system-generated status change note.
  // The detection helper uses prefix matching so notes with appended
  // admin reasons (e.g. "Application approved\n\nReason: ...") are
  // still recognized. Previously this component used exact-match
  // lookup which missed notes with reasons.
  const { isSystem, status } = detectSystemGeneratedNote(note.content);

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
