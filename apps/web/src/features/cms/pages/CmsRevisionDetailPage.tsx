// CmsRevisionDetailPage component
// Detailed revision history for a single CMS page

import React from 'react'
import { Container, Title, LoadingOverlay, Alert, Text, Stack, Button } from '@mantine/core'
import { IconAlertCircle, IconArrowLeft } from '@tabler/icons-react'
import { useParams, Link } from 'react-router-dom'
import { useCmsRevisions } from '../hooks/useCmsRevisions'
import { useCmsPageList } from '../hooks/useCmsPageList'
import { CmsRevisionCard } from '../components/CmsRevisionCard'

export const CmsRevisionDetailPage: React.FC = () => {
  const { pageId } = useParams<{ pageId: string }>()
  const { data: revisions, isLoading, error } = useCmsRevisions(Number(pageId))
  const { data: pages } = useCmsPageList()

  const page = pages?.find(p => p.id === Number(pageId))

  if (isLoading) {
    return (
      <Container size="lg" py="xl">
        <LoadingOverlay visible />
      </Container>
    )
  }

  if (error) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle />} color="red" title="Error">
          Failed to load revision history. Please try again later.
        </Alert>
      </Container>
    )
  }

  return (
    <Container size="lg" py="xl">
      <Button
        component={Link}
        to="/admin/cms/revisions"
        variant="subtle"
        leftSection={<IconArrowLeft size={16} />}
        mb="lg"
        size="md"
      >
        Back to List
      </Button>

      <Title order={1} mb="xl">
        {page?.title ? `${page.title} - Revision History` : 'Revision History'}
      </Title>

      {!revisions || revisions.length === 0 ? (
        <Text c="dimmed">No revisions found for this page.</Text>
      ) : (
        <Stack gap="md">
          {revisions.map((revision) => (
            <CmsRevisionCard key={revision.id} revision={revision} />
          ))}
        </Stack>
      )}
    </Container>
  )
}
