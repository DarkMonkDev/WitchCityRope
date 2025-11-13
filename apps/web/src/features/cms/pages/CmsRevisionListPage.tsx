// CmsRevisionListPage component
// Admin dashboard listing all CMS pages with revision counts

import React, { useState, useMemo } from 'react'
import {
  Container,
  Title,
  Table,
  Alert,
  Text,
  Paper,
  Stack,
  Box,
  Group,
  Anchor,
} from '@mantine/core'
import {
  IconAlertCircle,
  IconExternalLink,
  IconSortAscending,
  IconSortDescending,
} from '@tabler/icons-react'
import { useNavigate } from 'react-router-dom'
import { useCmsPageList } from '../hooks/useCmsPageList'

type SortField = 'title' | 'slug' | 'revisionCount' | 'updatedAt' | 'lastModifiedBy'
type SortDirection = 'asc' | 'desc'

export const CmsRevisionListPage: React.FC = () => {
  const navigate = useNavigate()
  const { data: pages, isLoading, error } = useCmsPageList()
  const [sortField, setSortField] = useState<SortField>('title')
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc')

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDirection('asc')
    }
  }

  const getSortIcon = (field: SortField) => {
    if (sortField !== field) return null
    return sortDirection === 'asc' ? (
      <IconSortAscending size={16} style={{ color: 'white' }} />
    ) : (
      <IconSortDescending size={16} style={{ color: 'white' }} />
    )
  }

  const sortedPages = useMemo(() => {
    if (!pages) return []

    const sorted = [...pages].sort((a, b) => {
      let aVal: any = a[sortField]
      let bVal: any = b[sortField]

      // Handle date sorting
      if (sortField === 'updatedAt') {
        aVal = new Date(aVal).getTime()
        bVal = new Date(bVal).getTime()
      }

      // Handle string sorting (case-insensitive)
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        aVal = aVal.toLowerCase()
        bVal = bVal.toLowerCase()
      }

      if (aVal < bVal) return sortDirection === 'asc' ? -1 : 1
      if (aVal > bVal) return sortDirection === 'asc' ? 1 : -1
      return 0
    })

    return sorted
  }, [pages, sortField, sortDirection])

  if (error) {
    return (
      <Container size="xl" py="xl">
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
    <Container size="xl" py="xl">
      {/* Header - Matching admin page style */}
      <Title
        order={1}
        mb="xs"
        style={{
          fontFamily: "'Montserrat', sans-serif",
          fontSize: '32px',
          fontWeight: 800,
          color: '#880124',
          textTransform: 'uppercase',
          letterSpacing: '-0.5px',
        }}
      >
        CMS Revision History
      </Title>

      <Stack gap="md">
        {/* CMS Pages Table - Matching admin table style */}
        <Paper shadow="sm" radius="md">
          <Table striped highlightOnHover>
            <Table.Thead
              style={{
                backgroundColor: '#880124',
                color: 'white',
              }}
            >
              <Table.Tr>
                <Table.Th
                  style={{
                    backgroundColor: '#880124',
                    borderBottom: 'none',
                    cursor: 'pointer',
                  }}
                  onClick={() => handleSort('title')}
                >
                  <Group gap={4} justify="flex-start">
                    <Text
                      fw={600}
                      size="sm"
                      style={{
                        color: 'white',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      PAGE NAME
                    </Text>
                    {getSortIcon('title')}
                  </Group>
                </Table.Th>
                <Table.Th
                  style={{
                    backgroundColor: '#880124',
                    borderBottom: 'none',
                    cursor: 'pointer',
                  }}
                  onClick={() => handleSort('slug')}
                >
                  <Group gap={4} justify="flex-start">
                    <Text
                      fw={600}
                      size="sm"
                      style={{
                        color: 'white',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      URL
                    </Text>
                    {getSortIcon('slug')}
                  </Group>
                </Table.Th>
                <Table.Th
                  style={{
                    backgroundColor: '#880124',
                    borderBottom: 'none',
                    textAlign: 'center',
                    cursor: 'pointer',
                  }}
                  onClick={() => handleSort('revisionCount')}
                >
                  <Group gap={4} justify="center">
                    <Text
                      fw={600}
                      size="sm"
                      style={{
                        color: 'white',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      TOTAL REVISIONS
                    </Text>
                    {getSortIcon('revisionCount')}
                  </Group>
                </Table.Th>
                <Table.Th
                  style={{
                    backgroundColor: '#880124',
                    borderBottom: 'none',
                    cursor: 'pointer',
                  }}
                  onClick={() => handleSort('updatedAt')}
                >
                  <Group gap={4} justify="flex-start">
                    <Text
                      fw={600}
                      size="sm"
                      style={{
                        color: 'white',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      LAST EDITED
                    </Text>
                    {getSortIcon('updatedAt')}
                  </Group>
                </Table.Th>
                <Table.Th
                  style={{
                    backgroundColor: '#880124',
                    borderBottom: 'none',
                    cursor: 'pointer',
                  }}
                  onClick={() => handleSort('lastModifiedBy')}
                >
                  <Group gap={4} justify="flex-start">
                    <Text
                      fw={600}
                      size="sm"
                      style={{
                        color: 'white',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      LAST EDITED BY
                    </Text>
                    {getSortIcon('lastModifiedBy')}
                  </Group>
                </Table.Th>
                <Table.Th
                  style={{
                    backgroundColor: '#880124',
                    borderBottom: 'none',
                    textAlign: 'center',
                  }}
                >
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    VIEW PAGE
                  </Text>
                </Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {sortedPages.map((page) => (
                <Table.Tr
                  key={page.id}
                  style={{ cursor: 'pointer' }}
                  onClick={(e) => {
                    // Don't navigate if clicking the View Page link
                    if ((e.target as HTMLElement).closest('a')) {
                      return
                    }
                    navigate(`/admin/cms/revisions/${page.id}`)
                  }}
                >
                  <Table.Td>
                    <Text size="sm" fw={600} style={{ color: '#2B2B2B' }}>
                      {page.title}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm" c="dimmed">
                      /{page.slug}
                    </Text>
                  </Table.Td>
                  <Table.Td style={{ textAlign: 'center' }}>
                    <Text size="sm" style={{ color: '#2B2B2B' }}>
                      {page.revisionCount}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm" style={{ color: '#2B2B2B' }}>
                      {formatDate(page.updatedAt)}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm" style={{ color: '#2B2B2B' }}>
                      {page.lastModifiedBy}
                    </Text>
                  </Table.Td>
                  <Table.Td style={{ textAlign: 'center' }}>
                    <Anchor
                      href={`/${page.slug}`}
                      onClick={(e) => e.stopPropagation()}
                      style={{
                        color: '#880124',
                        textDecoration: 'none',
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '4px',
                      }}
                    >
                      <Text size="sm" fw={500}>
                        View Page
                      </Text>
                      <IconExternalLink size={14} />
                    </Anchor>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>

          {/* Empty State */}
          {sortedPages.length === 0 && !isLoading && (
            <Box p="xl" ta="center">
              <Text c="dimmed" size="lg">
                No CMS pages found.
              </Text>
            </Box>
          )}

          {/* Loading State */}
          {isLoading && (
            <Box p="xl" ta="center">
              <Text c="dimmed">Loading CMS pages...</Text>
            </Box>
          )}
        </Paper>
      </Stack>
    </Container>
  )
}
