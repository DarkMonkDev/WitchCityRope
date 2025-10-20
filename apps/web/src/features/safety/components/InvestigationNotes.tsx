import React, { useState } from 'react';
import { Card, Title, Stack, Textarea, Checkbox, Button, Group, Text, Badge, Paper } from '@mantine/core';
import { IconNote, IconTrash } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { showNotification } from '@mantine/notifications';

enum IncidentNoteType {
  Manual = 'Manual',
  System = 'System'
}

interface IncidentNoteDto {
  id: string;
  content: string;
  authorName?: string;
  createdAt: string;
  type: IncidentNoteType;
  isPrivate: boolean;
}

interface InvestigationNotesProps {
  incidentId: string;
}

const formatDate = (date: string): string => {
  return new Date(date).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
};

export const InvestigationNotes: React.FC<InvestigationNotesProps> = ({ incidentId }) => {
  const [noteContent, setNoteContent] = useState('');
  const [isPrivate, setIsPrivate] = useState(false);
  const queryClient = useQueryClient();

  // Fetch notes: GET /api/safety/admin/incidents/{id}/notes
  const { data: notes } = useQuery<IncidentNoteDto[]>({
    queryKey: ['safety', 'notes', incidentId],
    queryFn: async () => {
      const response = await fetch(`/api/safety/admin/incidents/${incidentId}/notes`, {
        credentials: 'include'
      });
      if (!response.ok) throw new Error('Failed to fetch notes');
      return response.json();
    },
  });

  // Add note mutation: POST /api/safety/admin/incidents/{id}/notes
  const addNoteMutation = useMutation<unknown, Error, void>({
    mutationFn: async () => {
      const response = await fetch(`/api/safety/admin/incidents/${incidentId}/notes`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ content: noteContent, isPrivate }),
      });
      if (!response.ok) throw new Error('Failed to add note');
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'notes', incidentId] });
      setNoteContent('');
      setIsPrivate(false);
      showNotification({
        title: 'Success',
        message: 'Note added successfully',
        color: 'green',
      });
    },
    onError: (error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : 'Failed to add note',
        color: 'red',
      });
    }
  });

  // Delete note mutation: DELETE /api/safety/admin/notes/{noteId}
  const deleteNoteMutation = useMutation({
    mutationFn: async (noteId: string) => {
      const response = await fetch(`/api/safety/admin/notes/${noteId}`, {
        method: 'DELETE',
        credentials: 'include',
      });
      if (!response.ok) throw new Error('Failed to delete note');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'notes', incidentId] });
      showNotification({
        title: 'Success',
        message: 'Note deleted successfully',
        color: 'green',
      });
    },
    onError: (error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : 'Failed to delete note',
        color: 'red',
      });
    }
  });

  return (
    <Card p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
      <Title order={3} mb="md" style={{ color: '#880124', display: 'flex', alignItems: 'center', gap: '8px' }}>
        <IconNote size={20} />
        Investigation Notes
      </Title>

      {/* Add Note Form */}
      <Stack gap="md" mb="xl">
        <Textarea
          placeholder="Add investigation note..."
          data-testid="add-note-content"
          value={noteContent}
          onChange={(e) => setNoteContent(e.currentTarget.value)}
          minRows={3}
        />

        <Checkbox
          label="Private note (only visible to administrators)"
          data-testid="add-note-privacy"
          checked={isPrivate}
          onChange={(e) => setIsPrivate(e.currentTarget.checked)}
        />

        <Button
          onClick={() => addNoteMutation.mutate()}
          loading={addNoteMutation.isPending}
          disabled={!noteContent.trim()}
          data-testid="add-note-submit"
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
          Add Note
        </Button>
      </Stack>

      {/* Notes List */}
      <Stack gap="md" data-testid="notes-list">
        {notes && notes.length > 0 ? (
          notes.map((note) => (
            <Paper
              key={note.id}
              p="md"
              style={{ background: '#F5F5F5', borderRadius: '8px' }}
              data-testid={`note-${note.id}`}
            >
              <Group justify="space-between" mb="xs">
                <Group gap="xs">
                  <Text fw={600} size="sm">{note.authorName || 'System'}</Text>
                  {note.type === IncidentNoteType.System && (
                    <Badge color="gray" size="sm">System</Badge>
                  )}
                  {note.isPrivate && (
                    <Badge color="purple" size="sm">Private</Badge>
                  )}
                </Group>
                <Group gap="xs">
                  <Text size="xs" c="dimmed">{formatDate(note.createdAt)}</Text>
                  {note.type === IncidentNoteType.Manual && (
                    <Button
                      size="xs"
                      variant="subtle"
                      color="red"
                      onClick={() => deleteNoteMutation.mutate(note.id)}
                      data-testid={`delete-note-${note.id}`}
                      p={4}
                    >
                      <IconTrash size={14} />
                    </Button>
                  )}
                </Group>
              </Group>
              <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{note.content}</Text>
            </Paper>
          ))
        ) : (
          <Text c="dimmed" size="sm" ta="center" py="md">
            No notes added yet
          </Text>
        )}
      </Stack>
    </Card>
  );
};
