// CmsRevisionCard component
// Display single revision with metadata and expandable content

import React, { useState } from 'react'
import { Paper, Text, Group, Button, Box } from '@mantine/core'
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
    <Paper shadow="sm" px="xl" py="md" radius="md" withBorder>
      <Group justify="space-between" align="center" mb={8}>
        <Text size="sm" c="dimmed">
          {formattedDate} - {revision.createdBySceneName}
        </Text>
        <Button
          variant="subtle"
          size="xs"
          onClick={() => setShowFullContent(!showFullContent)}
          style={{
            minHeight: 'auto',
            height: 'auto',
            padding: '4px 8px'
          }}
        >
          {showFullContent ? 'Show Less' : 'View Full Content'}
        </Button>
      </Group>

      <Box
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
    </Paper>
  )
}
