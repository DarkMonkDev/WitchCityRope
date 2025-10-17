// CmsRevisionListPage component
// Admin dashboard listing all CMS pages with revision counts

import React from 'react'
import { Container, Title, Table, LoadingOverlay, Alert, Text } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useNavigate } from 'react-router-dom'
import { useCmsPageList } from '../hooks/useCmsPageList'

export const CmsRevisionListPage: React.FC = () => {
  const navigate = useNavigate()
  const { data: pages, isLoading, error } = useCmsPageList()

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
          Failed to load CMS pages. Please try again later.
        </Alert>
      </Container>
    )
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    })
  }

  return (
    <Container size="lg" py="xl">
      <Title order={1} mb="xl">
        CMS Revision History
      </Title>

      {!pages || pages.length === 0 ? (
        <Text c="dimmed">No CMS pages found.</Text>
      ) : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Page Name</Table.Th>
              <Table.Th>Total Revisions</Table.Th>
              <Table.Th>Last Edited</Table.Th>
              <Table.Th>Last Edited By</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {pages.map((page) => (
              <Table.Tr
                key={page.id}
                style={{ cursor: 'pointer' }}
                onClick={() => navigate(`/admin/cms/revisions/${page.id}`)}
              >
                <Table.Td>
                  <Text fw={500}>{page.title}</Text>
                  <Text size="sm" c="dimmed">
                    /{page.slug}
                  </Text>
                </Table.Td>
                <Table.Td>{page.revisionCount}</Table.Td>
                <Table.Td>{formatDate(page.updatedAt)}</Table.Td>
                <Table.Td>{page.lastModifiedBy}</Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}
    </Container>
  )
}
