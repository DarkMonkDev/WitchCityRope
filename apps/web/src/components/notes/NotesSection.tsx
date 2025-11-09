import React, { useState } from 'react';
import { Card, Stack, Title, Textarea, Group, Paper, Text } from '@mantine/core';
import { Button } from '@mantine/core';

interface NotesSectionProps<T extends { id?: string; createdAt?: string; timestamp?: string; content?: string }> {
  notes: T[];
  onSaveNote: (content: string) => Promise<void>;
  renderNoteHeader?: (note: T) => React.ReactNode;
  renderFullNote?: (note: T) => React.ReactNode;
  placeholder?: string;
  emptyMessage?: string;
  isSaving?: boolean;
  title?: string;
}

export function NotesSection<T extends { id?: string; createdAt?: string; timestamp?: string; content?: string }>({
  notes,
  onSaveNote,
  renderNoteHeader,
  renderFullNote,
  placeholder = 'Add a note...',
  emptyMessage = 'No notes added yet',
  isSaving = false,
  title = 'Notes',
}: NotesSectionProps<T>) {
  const [newNote, setNewNote] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Sort notes by date (newest first)
  // Support both createdAt and timestamp fields
  const sortedNotes = [...notes].sort((a, b) => {
    const dateA = (a.timestamp || a.createdAt) ? new Date(a.timestamp || a.createdAt || '').getTime() : 0;
    const dateB = (b.timestamp || b.createdAt) ? new Date(b.timestamp || b.createdAt || '').getTime() : 0;
    return dateB - dateA;
  });

  const handleSaveNote = async () => {
    if (!newNote.trim() || isSubmitting) return;

    setIsSubmitting(true);
    try {
      await onSaveNote(newNote.trim());
      setNewNote('');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Card p="xl">
      <Group justify="space-between" align="center" mb="md">
        <Title order={3} style={{ color: '#880124' }}>
          {title}
        </Title>
        <Button
          onClick={handleSaveNote}
          disabled={!newNote.trim() || isSubmitting || isSaving}
          loading={isSubmitting || isSaving}
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
          Save Note
        </Button>
      </Group>

      <Stack gap="md">
        {/* Add Note Text Area */}
        <Textarea
          placeholder={placeholder}
          value={newNote}
          onChange={(e) => setNewNote(e.currentTarget.value)}
          minRows={4}
          data-testid="add-note-textarea"
          styles={{
            input: {
              borderRadius: '8px',
              border: '1px solid #E0E0E0',
            },
          }}
        />

        {/* Notes List */}
        {sortedNotes.length > 0 ? (
          <Stack gap="sm">
            {sortedNotes.map((note, index) => {
              // If renderFullNote is provided, use it (for unified notes with complex rendering)
              if (renderFullNote) {
                return <React.Fragment key={note.id || index}>{renderFullNote(note)}</React.Fragment>;
              }

              // Otherwise, use the default rendering with optional header
              return (
                <Paper key={note.id || index} p="md" style={{ background: '#F5F5F5', borderRadius: '8px' }}>
                  {renderNoteHeader && renderNoteHeader(note)}
                  <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                    {note.content}
                  </Text>
                </Paper>
              );
            })}
          </Stack>
        ) : (
          <Text c="dimmed" size="sm" ta="center" py="md">
            {emptyMessage}
          </Text>
        )}
      </Stack>
    </Card>
  );
}
