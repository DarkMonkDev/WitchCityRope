import React, { useMemo } from 'react'
import { Table, Paper, Text, Group } from '@mantine/core'
import { IconArrowUp, IconArrowDown } from '@tabler/icons-react'

/**
 * Generic column definition for BaseEventsTable
 * @template T - The event DTO type (EventDto, UserEventDto, etc.)
 */
export interface TableColumn<T> {
  /** Unique key for the column */
  key: string
  /** Column header label */
  label: string
  /** Function to render cell content */
  render: (event: T, index: number) => React.ReactNode
  /** Whether column is sortable (currently only date column supports sorting) */
  sortable?: boolean
  /** Text alignment */
  align?: 'left' | 'center' | 'right'
  /** Visibility breakpoint (hides column below this breakpoint) */
  visibleFrom?: 'sm' | 'md' | 'lg'
  /** Minimum column width */
  minWidth?: string
  /** Additional styles for header */
  headerStyle?: React.CSSProperties
  /** Additional styles for cells */
  cellStyle?: React.CSSProperties
}

interface BaseEventsTableProps<T> {
  /** Array of events to display */
  events: T[]
  /** Column definitions */
  columns: TableColumn<T>[]
  /** Current sort order value (e.g., 'date-oldest', 'date-newest') */
  sortBy?: string
  /** Callback to update sort order */
  setSortBy?: (sortBy: string) => void
  /** Callback when row is clicked */
  onRowClick?: (event: T) => void
  /** Maximum table width */
  maxWidth?: string
}

/**
 * BaseEventsTable - Shared table component for event listings
 *
 * Used by:
 * - Dashboard My Events page (UserEventDto)
 * - Public Events List page (EventDto)
 *
 * Features:
 * - Generic type support for different event DTOs
 * - Configurable columns via props
 * - Built-in sorting for date column
 * - Responsive design (Action column hidden on mobile)
 * - Burgundy header with white text
 * - Striped rows with hover effect
 * - Responsive font sizes (media queries)
 */
export function BaseEventsTable<T>({
  events,
  columns,
  sortBy,
  setSortBy,
  onRowClick,
  maxWidth = '1200px',
}: BaseEventsTableProps<T>): React.ReactElement {
  // Handle sort toggle for date column
  const handleSort = () => {
    if (setSortBy && sortBy) {
      // Toggle between 'date-oldest' and 'date-newest'
      setSortBy(sortBy === 'date-oldest' ? 'date-newest' : 'date-oldest')
    }
  }

  // Sort events if sortBy is provided
  const sortedEvents = useMemo(() => {
    if (!sortBy || !events || events.length === 0) return events

    const sorted = [...events]
    // Note: Sorting is handled by parent component based on sortBy value
    // This component just displays pre-sorted data
    return sorted
  }, [events, sortBy])

  return (
    <>
      {/* Responsive CSS for table font sizes */}
      <style>{`
        /* Table responsive font sizes */
        @media (max-width: 767px) {
          .table-header-text {
            font-size: 12px !important;
          }
          .table-cell-text {
            font-size: 14px !important;
          }
          .table-badge {
            font-size: 12px !important;
          }
        }
      `}</style>

      <Paper shadow="sm" radius="md" style={{ maxWidth, margin: '0 auto' }}>
        <Table striped highlightOnHover>
          {/* Table Header - Burgundy background with white text */}
          <Table.Thead
            style={{
              backgroundColor: '#880124',
              color: 'white',
            }}
          >
            <Table.Tr>
              {columns.map((column) => (
                <Table.Th
                  key={column.key}
                  visibleFrom={column.visibleFrom}
                  onClick={column.sortable ? handleSort : undefined}
                  style={{
                    backgroundColor: '#880124',
                    borderBottom: 'none',
                    cursor: column.sortable ? 'pointer' : 'default',
                    textAlign: column.align || 'left',
                    minWidth: column.minWidth,
                    paddingLeft: column.key === 'date' ? '24px' : undefined,
                    ...column.headerStyle,
                  }}
                >
                  <Group gap={4} justify={column.align === 'center' ? 'center' : 'flex-start'}>
                    <Text
                      fw={600}
                      size="sm"
                      className="table-header-text"
                      style={{
                        color: 'white',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      {column.label}
                    </Text>
                    {column.sortable && sortBy && (
                      sortBy === 'date-oldest' ? (
                        <IconArrowUp size={16} color="white" />
                      ) : (
                        <IconArrowDown size={16} color="white" />
                      )
                    )}
                  </Group>
                </Table.Th>
              ))}
            </Table.Tr>
          </Table.Thead>

          {/* Table Body */}
          <Table.Tbody>
            {sortedEvents.map((event, index) => (
              <Table.Tr
                key={(event as any).id || index}
                onClick={() => onRowClick?.(event)}
                style={{
                  cursor: onRowClick ? 'pointer' : 'default',
                }}
              >
                {columns.map((column) => (
                  <Table.Td
                    key={column.key}
                    visibleFrom={column.visibleFrom}
                    style={{
                      textAlign: column.align || 'left',
                      paddingLeft: column.key === 'date' ? '24px' : undefined,
                      ...column.cellStyle,
                    }}
                  >
                    {column.render(event, index)}
                  </Table.Td>
                ))}
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Paper>
    </>
  )
}
