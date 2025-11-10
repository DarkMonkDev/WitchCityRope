import React, { useState } from 'react';
import { Card, Title, Stack, Textarea, Button, Group, Text, Badge, Paper, Modal, Anchor } from '@mantine/core';
import { IconNote, IconTrash, IconEdit } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { showNotification } from '@mantine/notifications';
import { modals } from '@mantine/modals';
import type { components } from '@witchcityrope/shared-types';

// Use auto-generated types from API
type IncidentNoteDto = components['schemas']['IncidentNoteDto'];
type IncidentNoteType = components['schemas']['IncidentNoteType'];

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
  const [editingNoteId, setEditingNoteId] = useState<string | null>(null);
  const [editingContent, setEditingContent] = useState('');
  const queryClient = useQueryClient();

  // Fetch notes: GET /api/safety/admin/incidents/{id}/notes
  const { data: notesResponse } = useQuery<{ notes: IncidentNoteDto[] }>({
    queryKey: ['safety', 'notes', incidentId],
    queryFn: async () => {
      const response = await fetch(`/api/safety/admin/incidents/${incidentId}/notes`, {
        credentials: 'include'
      });
      if (!response.ok) throw new Error('Failed to fetch notes');
      return response.json();
    },
  });

  const notes = notesResponse?.notes;

  // Add note mutation: POST /api/safety/admin/incidents/{id}/notes
  const addNoteMutation = useMutation<unknown, Error, void>({
    mutationFn: async () => {
      const response = await fetch(`/api/safety/admin/incidents/${incidentId}/notes`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ content: noteContent }),
      });
      if (!response.ok) throw new Error('Failed to add note');
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'notes', incidentId] });
      setNoteContent('');
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

  // Update note mutation: PUT /api/safety/admin/notes/{noteId}
  const updateNoteMutation = useMutation({
    mutationFn: async ({ noteId, content }: { noteId: string; content: string }) => {
      const response = await fetch(`/api/safety/admin/notes/${noteId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ content }),
      });
      if (!response.ok) throw new Error('Failed to update note');
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'notes', incidentId] });
      setEditingNoteId(null);
      setEditingContent('');
      showNotification({
        title: 'Success',
        message: 'Note updated successfully',
        color: 'green',
      });
    },
    onError: (error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : 'Failed to update note',
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

  const handleDeleteNote = (noteId: string) => {
    modals.openConfirmModal({
      title: 'Delete Investigation Note',
      children: (
        <Text size="sm">
          Are you sure you want to delete this note? This action cannot be undone.
        </Text>
      ),
      labels: { confirm: 'Delete', cancel: 'Cancel' },
      confirmProps: { color: 'red' },
      onConfirm: () => deleteNoteMutation.mutate(noteId),
    });
  };

  const handleEditNote = (note: IncidentNoteDto) => {
    setEditingNoteId(note.id || null);
    setEditingContent(note.content || '');
  };

  const handleSaveEdit = (noteId: string) => {
    updateNoteMutation.mutate({ noteId, content: editingContent });
  };

  const handleCancelEdit = () => {
    setEditingNoteId(null);
    setEditingContent('');
  };

  return (
    <Card p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
      <Group justify="space-between" align="center" mb="md">
        <Title order={3} style={{ color: '#880124', display: 'flex', alignItems: 'center', gap: '8px' }}>
          <IconNote size={20} />
          Investigation Notes
        </Title>
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
      </Group>

      {/* Add Note Form */}
      <Stack gap="md" mb="xl">
        <Textarea
          placeholder="Add investigation note..."
          data-testid="add-note-content"
          value={noteContent}
          onChange={(e) => setNoteContent(e.currentTarget.value)}
          minRows={3}
        />
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
              <Group justify="space-between">
                <Group gap="xs">
                  <Text fw={600} size="sm">{note.authorName || 'System'}</Text>
                  {note.type === 'System' && (
                    <Badge color="gray" size="sm">System</Badge>
                  )}
                  {note.type === 'Manual' && (
                    <Anchor
                      size="sm"
                      onClick={() => handleEditNote(note)}
                      style={{ cursor: 'pointer' }}
                      data-testid={`edit-note-${note.id}`}
                    >
                      edit
                    </Anchor>
                  )}
                </Group>
                <Group gap="xs">
                  <Text size="xs" c="dimmed">{formatDate(note.createdAt)}</Text>
                  {note.type === 'Manual' && (
                    <Button
                      size="xs"
                      variant="subtle"
                      color="red"
                      onClick={() => handleDeleteNote(note.id!)}
                      data-testid={`delete-note-${note.id}`}
                      p={4}
                    >
                      <IconTrash size={14} />
                    </Button>
                  )}
                </Group>
              </Group>

              {editingNoteId === note.id ? (
                <Stack gap="sm" mt="sm">
                  <Textarea
                    value={editingContent}
                    onChange={(e) => setEditingContent(e.currentTarget.value)}
                    minRows={3}
                    data-testid={`edit-note-textarea-${note.id}`}
                  />
                  <Group gap="sm">
                    <Button
                      size="xs"
                      onClick={() => handleSaveEdit(note.id!)}
                      loading={updateNoteMutation.isPending}
                      data-testid={`save-note-${note.id}`}
                    >
                      Save
                    </Button>
                    <Button
                      size="xs"
                      variant="subtle"
                      onClick={handleCancelEdit}
                      data-testid={`cancel-edit-note-${note.id}`}
                    >
                      Cancel
                    </Button>
                  </Group>
                </Stack>
              ) : (
                <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{note.content}</Text>
              )}
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
