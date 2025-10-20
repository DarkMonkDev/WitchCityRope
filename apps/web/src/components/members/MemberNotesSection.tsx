import React, { useState } from 'react'
import { Title, Stack, Paper, Text, Group, Card, Textarea } from '@mantine/core'
import { IconNotes } from '@tabler/icons-react'
import { notifications } from '@mantine/notifications'
import type { UserNoteResponse } from '../../lib/api/types/member-details.types'
import { useCreateMemberNote } from '../../lib/api/hooks/useMemberDetails'

interface MemberNotesSectionProps {
  memberId: string
  notes: UserNoteResponse[]
}

// Format time helper function (matches VettingApplicationDetail.tsx)
const formatTime = (dateString: string) => {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays < 7) return `${diffDays}d ago`

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
  })
}

export const MemberNotesSection: React.FC<MemberNotesSectionProps> = ({ memberId, notes }) => {
  const [newNote, setNewNote] = useState('')
  const createNoteMutation = useCreateMemberNote()

  // Sort notes by date (newest first)
  const sortedNotes = [...notes].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  )

  const handleSaveNote = async () => {
    if (!newNote.trim()) {
      return
    }

    try {
      await createNoteMutation.mutateAsync({
        userId: memberId,
        request: {
          content: newNote.trim(),
        },
      })

      // Clear the textarea after successful save
      setNewNote('')

      notifications.show({
        title: 'Success',
        message: 'Note added successfully',
        color: 'green',
      })
    } catch (error) {
      notifications.show({
        title: 'Error',
        message: 'Failed to add note. Please try again.',
        color: 'red',
      })
    }
  }

  return (
    <Card p="xl">
      <Group justify="space-between" align="center" mb="md">
        <Title order={3} style={{ color: '#880124' }}>
          Notes
        </Title>
        <button
          className={newNote.trim() ? "btn btn-primary" : "btn"}
          onClick={handleSaveNote}
          disabled={!newNote.trim()}
          data-testid="save-note-button"
          type="button"
        >
          Save Note
        </button>
      </Group>

      <Stack gap="md">
        {/* Add Note Text Area */}
        <Textarea
          placeholder="Add a note about this member..."
          value={newNote}
          onChange={(e) => setNewNote(e.currentTarget.value)}
          minRows={4}
          data-testid="add-note-textarea"
          styles={{
            input: {
              borderRadius: '8px',
              border: '1px solid #E0E0E0',
            }
          }}
        />

        {/* Notes List */}
        {sortedNotes.length > 0 ? (
          <Stack gap="sm">
            {sortedNotes.map((note) => (
              <Paper key={note.id} p="md" style={{ background: '#F5F5F5', borderRadius: '8px' }}>
                <Group justify="space-between" mb="xs">
                  <Group gap="xs">
                    <IconNotes size={16} style={{ color: '#880124' }} />
                    <Text fw={600} size="sm">
                      {note.authorSceneName || 'Unknown'}
                    </Text>
                  </Group>
                  <Text size="sm" c="dimmed">
                    {formatTime(note.createdAt)}
                  </Text>
                </Group>
                <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                  {note.content}
                </Text>
              </Paper>
            ))}
          </Stack>
        ) : (
          <Text c="dimmed" size="sm" ta="center" py="md">
            No notes added yet
          </Text>
        )}
      </Stack>
    </Card>
  )
}
