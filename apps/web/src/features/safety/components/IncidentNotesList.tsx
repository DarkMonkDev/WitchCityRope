import React, { useState } from 'react';
import {
  Paper,
  Stack,
  Group,
  Text,
  Textarea,
  Badge,
  Title,
  Switch,
  TextInput
} from '@mantine/core';
import { IconNotes, IconLock, IconWorld } from '@tabler/icons-react';

export enum IncidentNoteType {
  Manual = 1,
  System = 2
}

export interface IncidentNoteDto {
  id: string;
  incidentId: string;
  authorId: string;
  authorName: string;
  content: string;
  noteType: IncidentNoteType;
  isPrivate: boolean;
  tags?: string[];
  createdAt: string;
  updatedAt?: string;
}

interface IncidentNotesListProps {
  notes: IncidentNoteDto[];
  onAddNote: (content: string, isPrivate: boolean, tags?: string[]) => Promise<void>;
  isAddingNote?: boolean;
}

const formatTime = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffMins < 1) return 'just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays === 1) return 'yesterday';
  if (diffDays < 7) return `${diffDays}d ago`;

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
  });
};

export const IncidentNotesList: React.FC<IncidentNotesListProps> = ({
  notes,
  onAddNote,
  isAddingNote = false
}) => {
  const [newNote, setNewNote] = useState('');
  const [isPrivate, setIsPrivate] = useState(true);
  const [tags, setTags] = useState('');

  const handleSaveNote = async () => {
    if (!newNote.trim()) return;

    const tagArray = tags
      .split(',')
      .map(t => t.trim())
      .filter(t => t.length > 0);

    await onAddNote(newNote.trim(), isPrivate, tagArray.length > 0 ? tagArray : undefined);

    // Reset form
    setNewNote('');
    setIsPrivate(true);
    setTags('');
  };

  // Sort notes chronologically (newest first)
  const sortedNotes = [...notes].sort((a, b) =>
    new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  );

  return (
    <Paper p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
      <Group justify="space-between" align="center" mb="md">
        <Title order={3} style={{ color: '#880124' }}>
          Notes
        </Title>
        <button
          className={newNote.trim() ? "btn btn-primary" : "btn"}
          onClick={handleSaveNote}
          disabled={!newNote.trim() || isAddingNote}
          data-testid="save-note-button"
          type="button"
        >
          {isAddingNote ? 'Saving...' : 'Save Note'}
        </button>
      </Group>

      <Stack gap="md">
        {/* Add Note Form */}
        <Stack gap="sm">
          <Textarea
            placeholder="Add a note about this incident..."
            value={newNote}
            onChange={(e) => setNewNote(e.currentTarget.value)}
            minRows={4}
            disabled={isAddingNote}
            data-testid="add-note-textarea"
            styles={{
              input: {
                borderRadius: '8px',
                border: '1px solid #E0E0E0',
              }
            }}
          />

          <Group gap="md">
            <Switch
              label={isPrivate ? 'Private (coordinators only)' : 'Shared (visible to reporter if identified)'}
              checked={isPrivate}
              onChange={(e) => setIsPrivate(e.currentTarget.checked)}
              disabled={isAddingNote}
              styles={{
                label: {
                  fontSize: '14px',
                  fontWeight: 500
                }
              }}
            />
          </Group>

          <TextInput
            placeholder="Tags (optional, comma-separated)"
            value={tags}
            onChange={(e) => setTags(e.currentTarget.value)}
            disabled={isAddingNote}
            styles={{
              input: {
                borderRadius: '8px',
                border: '1px solid #E0E0E0'
              }
            }}
          />
        </Stack>

        {/* Notes List */}
        {sortedNotes.length > 0 ? (
          <Stack gap="sm">
            {sortedNotes.map((note) => {
              const isSystem = note.noteType === IncidentNoteType.System;

              return (
                <Paper
                  key={note.id}
                  p="md"
                  style={{
                    background: isSystem ? '#F0EDFF' : '#F5F5F5',
                    borderRadius: '8px',
                    borderLeft: isSystem ? '4px solid #7B2CBF' : 'none'
                  }}
                  data-testid={isSystem ? "system-note" : "manual-note"}
                >
                  <Group justify="space-between" mb="xs">
                    <Group gap="xs">
                      {/* System badge or note icon */}
                      {isSystem ? (
                        <Badge color="purple" size="sm">SYSTEM</Badge>
                      ) : (
                        <>
                          <IconNotes size={16} style={{ color: '#880124' }} />
                          {note.isPrivate ? (
                            <Badge color="gray" size="xs" leftSection={<IconLock size={10} />}>
                              Private
                            </Badge>
                          ) : (
                            <Badge color="blue" size="xs" leftSection={<IconWorld size={10} />}>
                              Shared
                            </Badge>
                          )}
                        </>
                      )}
                      <Text fw={600} size="sm">{note.authorName}</Text>
                    </Group>
                    <Text size="sm" c="dimmed">
                      {formatTime(note.createdAt)}
                      {note.updatedAt && note.updatedAt !== note.createdAt && <> • Edited</>}
                    </Text>
                  </Group>

                  <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
                    {note.content}
                  </Text>

                  {/* Tags */}
                  {note.tags && note.tags.length > 0 && (
                    <Group gap="xs" mt="xs">
                      {note.tags.map((tag, idx) => (
                        <Badge key={idx} size="sm" variant="light" color="gray">
                          {tag}
                        </Badge>
                      ))}
                    </Group>
                  )}
                </Paper>
              );
            })}
          </Stack>
        ) : (
          <Text c="dimmed" size="sm" ta="center" py="md">
            No notes added yet
          </Text>
        )}
      </Stack>
    </Paper>
  );
};
