import React from 'react'
import { Modal, Stack, Select, Textarea, Text, Group } from '@mantine/core'
import { useForm } from '@mantine/form'
import { notifications } from '@mantine/notifications'
import { WCRButton } from '../ui'
import { useCreateMemberNote } from '../../lib/api/hooks/useMemberDetails'

interface AddNoteModalProps {
  opened: boolean
  onClose: () => void
  memberId: string
}

interface NoteFormData {
  noteType: string
  content: string
}

const NOTE_TYPES = [
  { value: 'General', label: 'General Note' },
  { value: 'Vetting', label: 'Vetting Note' },
  { value: 'Administrative', label: 'Administrative Note' },
]

export const AddNoteModal: React.FC<AddNoteModalProps> = ({ opened, onClose, memberId }) => {
  const createNoteMutation = useCreateMemberNote()

  const form = useForm<NoteFormData>({
    initialValues: {
      noteType: 'General',
      content: '',
    },
    validate: {
      noteType: (value) => (!value ? 'Note type is required' : null),
      content: (value) => {
        if (!value || value.trim().length === 0) return 'Note content is required'
        if (value.trim().length < 10) return 'Note must be at least 10 characters'
        return null
      },
    },
  })

  const handleSubmit = form.onSubmit(async (values) => {
    try {
      await createNoteMutation.mutateAsync({
        userId: memberId,
        request: {
          noteType: values.noteType,
          content: values.content.trim(),
        },
      })

      notifications.show({
        title: 'Success',
        message: 'Note created successfully',
        color: 'green',
      })

      form.reset()
      onClose()
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error.message || 'Failed to create note',
        color: 'red',
      })
    }
  })

  return (
    <Modal
      opened={opened}
      onClose={() => {
        form.reset()
        onClose()
      }}
      title={<Text fw={600} size="lg">Add New Note</Text>}
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <Select
            label="Note Type"
            placeholder="Select note type..."
            data={NOTE_TYPES}
            required
            {...form.getInputProps('noteType')}
          />

          <Textarea
            label="Note Content"
            placeholder="Enter note content..."
            required
            minRows={6}
            maxRows={12}
            {...form.getInputProps('content')}
          />

          <Group justify="flex-end" mt="md">
            <WCRButton
              variant="outline"
              onClick={() => {
                form.reset()
                onClose()
              }}
            >
              Cancel
            </WCRButton>
            <WCRButton
              type="submit"
              variant="primary"
              loading={createNoteMutation.isPending}
            >
              Save Note
            </WCRButton>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}
