/**
 * TicketStatusBadge
 *
 * Color-coded status badge for ticket assignment status.
 * Used within event cards on the dashboard to show the status
 * of each ticket the user owns or has been assigned.
 *
 * Design reference: ui-design.md Screen 5 - TicketStatusBadge Component
 *
 * Status -> Color -> Text mapping:
 * - Active (own ticket)           -> green  -> "Active (You)"
 * - Active (accepted by assignee) -> green  -> "Accepted: [SceneName]"
 * - PendingAcceptance            -> yellow -> "Pending: [SceneName]"
 * - Declined                     -> red    -> "Declined"
 * - Unassigned                   -> gray   -> "Unassigned"
 */

import React from 'react'
import { Badge } from '@mantine/core'

interface TicketStatusBadgeProps {
  /** The status of the ticket assignment */
  status: string
  /** Whether this is the user's own ticket (not assigned to someone else) */
  isOwnTicket?: boolean
  /** The scene name of the person the ticket is assigned to */
  assigneeSceneName?: string
}

export const TicketStatusBadge: React.FC<TicketStatusBadgeProps> = ({
  status,
  isOwnTicket = false,
  assigneeSceneName,
}) => {
  const { color, label } = getBadgeConfig(status, isOwnTicket, assigneeSceneName)

  return (
    <Badge
      color={color}
      variant="light"
      size="sm"
      data-testid="ticket-status-badge"
    >
      {label}
    </Badge>
  )
}

TicketStatusBadge.displayName = 'TicketStatusBadge'

/**
 * Determine badge color and label text based on ticket status and ownership.
 */
function getBadgeConfig(
  status: string,
  isOwnTicket: boolean,
  assigneeSceneName?: string
): { color: string; label: string } {
  const normalizedStatus = status.toLowerCase()

  if (normalizedStatus === 'active') {
    if (isOwnTicket) {
      return { color: 'green', label: 'Active (You)' }
    }
    return {
      color: 'green',
      label: assigneeSceneName ? `Accepted: ${assigneeSceneName}` : 'Accepted',
    }
  }

  if (normalizedStatus === 'pendingacceptance') {
    return {
      color: 'yellow',
      label: assigneeSceneName ? `Pending: ${assigneeSceneName}` : 'Pending Acceptance',
    }
  }

  if (normalizedStatus === 'declined') {
    return { color: 'red', label: 'Declined' }
  }

  if (normalizedStatus === 'unassigned') {
    return { color: 'gray', label: 'Unassigned' }
  }

  // Fallback for unknown statuses
  return { color: 'gray', label: status }
}
