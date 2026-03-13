// CmsRevisionListPage component
// Admin page listing all CMS-managed content pages with revision counts.
// Provides create (modal), view (link), revision history (row click), and delete (modal) functionality.
// Title: "Content Management Pages"
// Column order: View Page | Page Name | URL | Total Revisions | Last Edited | Last Edited By | Delete

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
  Button,
} from '@mantine/core'
import {
  IconAlertCircle,
  IconExternalLink,
  IconSortAscending,
  IconSortDescending,
  IconPlus,
} from '@tabler/icons-react'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { useCmsPageList } from '../hooks/useCmsPageList'
import { CreateCmsPageModal } from '../components/CreateCmsPageModal'
import { DeleteCmsPageModal } from '../components/DeleteCmsPageModal'

type SortField = 'title' | 'slug' | 'revisionCount' | 'updatedAt' | 'lastModifiedBy'
type SortDirection = 'asc' | 'desc'

export const CmsRevisionListPage: React.FC = () => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { data: pages, isLoading, error } = useCmsPageList()
  const [sortField, setSortField] = useState<SortField>('title')
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc')

  // Modal state for create page
  const [createModalOpened, setCreateModalOpened] = useState(false)

  // Modal state for delete page
  const [deleteModalOpened, setDeleteModalOpened] = useState(false)
  const [deleteTargetId, setDeleteTargetId] = useState<number | null>(null)
  const [deleteTargetTitle, setDeleteTargetTitle] = useState('')

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

  // Invalidate and refetch the CMS page list after create or delete
  const handleMutationSuccess = () => {
    queryClient.invalidateQueries({ queryKey: ['cms-pages'] })
  }

  const handleDeleteClick = (e: React.MouseEvent, pageId: number, pageTitle: string) => {
    // Prevent row click from navigating to revision detail
    e.stopPropagation()
    setDeleteTargetId(pageId)
    setDeleteTargetTitle(pageTitle)
    setDeleteModalOpened(true)
  }

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

  // Shared style for table header cells (branded #880124 background)
  const thStyle: React.CSSProperties = {
    backgroundColor: '#880124',
    borderBottom: 'none',
    cursor: 'pointer',
  }

  // Non-sortable header cell style (no pointer cursor)
  const thStyleStatic: React.CSSProperties = {
    backgroundColor: '#880124',
    borderBottom: 'none',
  }

  // Shared style for header text (white, uppercase, letter-spaced)
  const thTextProps = {
    fw: 600 as const,
    size: 'sm' as const,
    style: {
      color: 'white',
      textTransform: 'uppercase' as const,
      letterSpacing: '0.5px',
    },
  }

  return (
    <Container size="xl" py="xl">
      {/* Header row - title on left, create button on right */}
      <Group justify="space-between" align="center" mb="xs">
        <Title
          order={1}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Content Management Pages
        </Title>

        <Button
          leftSection={<IconPlus size={18} />}
          color="green"
          onClick={() => setCreateModalOpened(true)}
          data-testid="create-new-page-button"
          style={{
            minHeight: 40,
            height: 'auto',
            padding: '10px 20px',
            lineHeight: 1.4,
          }}
        >
          Create New Page
        </Button>
      </Group>

      <Stack gap="md">
        {/* CMS Pages Table */}
        <Paper shadow="sm" radius="md">
          <Table striped highlightOnHover>
            <Table.Thead
              style={{
                backgroundColor: '#880124',
                color: 'white',
              }}
            >
              <Table.Tr>
                {/* VIEW PAGE - first column (non-sortable) */}
                <Table.Th style={{ ...thStyleStatic, textAlign: 'center' }}>
                  <Text {...thTextProps}>VIEW PAGE</Text>
                </Table.Th>

                {/* PAGE NAME - sortable */}
                <Table.Th style={thStyle} onClick={() => handleSort('title')}>
                  <Group gap={4} justify="flex-start">
                    <Text {...thTextProps}>PAGE NAME</Text>
                    {getSortIcon('title')}
                  </Group>
                </Table.Th>

                {/* URL - sortable */}
                <Table.Th style={thStyle} onClick={() => handleSort('slug')}>
                  <Group gap={4} justify="flex-start">
                    <Text {...thTextProps}>URL</Text>
                    {getSortIcon('slug')}
                  </Group>
                </Table.Th>

                {/* TOTAL REVISIONS - sortable */}
                <Table.Th
                  style={{ ...thStyle, textAlign: 'center' }}
                  onClick={() => handleSort('revisionCount')}
                >
                  <Group gap={4} justify="center">
                    <Text {...thTextProps}>TOTAL REVISIONS</Text>
                    {getSortIcon('revisionCount')}
                  </Group>
                </Table.Th>

                {/* LAST EDITED - sortable */}
                <Table.Th style={thStyle} onClick={() => handleSort('updatedAt')}>
                  <Group gap={4} justify="flex-start">
                    <Text {...thTextProps}>LAST EDITED</Text>
                    {getSortIcon('updatedAt')}
                  </Group>
                </Table.Th>

                {/* LAST EDITED BY - sortable */}
                <Table.Th style={thStyle} onClick={() => handleSort('lastModifiedBy')}>
                  <Group gap={4} justify="flex-start">
                    <Text {...thTextProps}>LAST EDITED BY</Text>
                    {getSortIcon('lastModifiedBy')}
                  </Group>
                </Table.Th>

                {/* DELETE - last column (non-sortable) */}
                <Table.Th style={{ ...thStyleStatic, textAlign: 'center' }}>
                  <Text {...thTextProps}>DELETE</Text>
                </Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {sortedPages.map((page) => (
                <Table.Tr
                  key={page.id}
                  style={{ cursor: 'pointer' }}
                  onClick={(e) => {
                    // Don't navigate if clicking the View Page link or Delete link
                    if ((e.target as HTMLElement).closest('a, button')) {
                      return
                    }
                    navigate(`/admin/cms/revisions/${page.id}`)
                  }}
                >
                  {/* View Page - first column */}
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

                  {/* Page Name */}
                  <Table.Td>
                    <Text size="sm" fw={600} style={{ color: '#2B2B2B' }}>
                      {page.title}
                    </Text>
                  </Table.Td>

                  {/* URL */}
                  <Table.Td>
                    <Text size="sm" c="dimmed">
                      /{page.slug}
                    </Text>
                  </Table.Td>

                  {/* Total Revisions */}
                  <Table.Td style={{ textAlign: 'center' }}>
                    <Text size="sm" style={{ color: '#2B2B2B' }}>
                      {page.revisionCount}
                    </Text>
                  </Table.Td>

                  {/* Last Edited */}
                  <Table.Td>
                    <Text size="sm" style={{ color: '#2B2B2B' }}>
                      {formatDate(page.updatedAt || '')}
                    </Text>
                  </Table.Td>

                  {/* Last Edited By */}
                  <Table.Td>
                    <Text size="sm" style={{ color: '#2B2B2B' }}>
                      {page.lastModifiedBy}
                    </Text>
                  </Table.Td>

                  {/* Delete - last column */}
                  <Table.Td style={{ textAlign: 'center' }}>
                    <Anchor
                      component="button"
                      type="button"
                      onClick={(e) => handleDeleteClick(e, page.id!, page.title ?? '')}
                      style={{
                        color: '#c92a2a',
                        textDecoration: 'none',
                        cursor: 'pointer',
                        fontSize: '14px',
                        fontWeight: 500,
                      }}
                      data-testid={`delete-page-${page.id}`}
                    >
                      Delete
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

      {/* Create New Page Modal */}
      <CreateCmsPageModal
        opened={createModalOpened}
        onClose={() => setCreateModalOpened(false)}
        onSuccess={handleMutationSuccess}
      />

      {/* Delete Page Confirmation Modal */}
      <DeleteCmsPageModal
        opened={deleteModalOpened}
        onClose={() => {
          setDeleteModalOpened(false)
          setDeleteTargetId(null)
          setDeleteTargetTitle('')
        }}
        pageId={deleteTargetId}
        pageTitle={deleteTargetTitle}
        onSuccess={handleMutationSuccess}
      />
    </Container>
  )
}
