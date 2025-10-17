// CmsRevisionCard component
// Display single revision with metadata and expandable content

import React, { useState } from 'react'
import { Paper, Text, Group, Button, Box, Badge } from '@mantine/core'
import { IconClock, IconUser } from '@tabler/icons-react'
import type { ContentRevisionDto } from '../types'

interface CmsRevisionCardProps {
  revision: ContentRevisionDto
}

export const CmsRevisionCard: React.FC<CmsRevisionCardProps> = ({ revision }) => {
  const [showFullContent, setShowFullContent] = useState(false)

  const formattedDate = new Date(revision.createdAt).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })

  return (
    <Paper shadow="sm" p="md" radius="md" withBorder>
      <Group justify="space-between" mb="xs">
        <Group>
          <IconClock size={16} />
          <Text size="sm" c="dimmed">
            {formattedDate}
          </Text>
        </Group>
        <Group>
          <IconUser size={16} />
          <Text size="sm" fw={500}>
            {revision.createdBy}
          </Text>
        </Group>
      </Group>

      {revision.changeDescription && (
        <Badge color="blue" variant="light" mb="sm">
          {revision.changeDescription}
        </Badge>
      )}

      <Box mb="sm">
        <Text size="sm" c="dimmed" mb="xs">
          Preview:
        </Text>
        <div
          style={{
            fontSize: '14px',
            lineHeight: 1.6,
            maxHeight: showFullContent ? 'none' : '4.8em',
            overflow: 'hidden',
            display: '-webkit-box',
            WebkitLineClamp: showFullContent ? 'unset' : 3,
            WebkitBoxOrient: 'vertical',
          }}
          dangerouslySetInnerHTML={{ __html: revision.contentPreview || '' }}
        />
      </Box>

      <Button
        variant="subtle"
        size="xs"
        onClick={() => setShowFullContent(!showFullContent)}
      >
        {showFullContent ? 'Show Less' : 'View Full Content'}
      </Button>
    </Paper>
  )
}
