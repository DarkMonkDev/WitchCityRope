import React from 'react'
import { Box } from '@mantine/core'
import { checkInTheme } from '../styles/theme'

interface CheckInStatsBarProps {
  stats: {
    notArrived: number
    total: number
    needWaiver: number
    checkedIn: number
  }
}

/**
 * CheckInStatsBar Component
 * Source: /docs/design/wireframes/event-checkin-visual.html (lines 539-557)
 *
 * Displays 4 stat boxes in a row with large numbers and labels
 * Matches wireframe typography and spacing exactly
 */
export const CheckInStatsBar: React.FC<CheckInStatsBarProps> = ({ stats }) => {
  const statItems = [
    { label: 'Not Arrived', value: stats.notArrived },
    { label: 'Total Registered', value: stats.total },
    { label: 'Need Waiver', value: stats.needWaiver },
    { label: 'Checked In', value: stats.checkedIn },
  ]

  return (
    <Box
      style={{
        background: checkInTheme.colors.ivory,
        padding: `${checkInTheme.spacing['2xl']} ${checkInTheme.spacing.xl}`,
        display: 'flex',
        justifyContent: 'space-around',
        borderBottom: `2px solid ${checkInTheme.colors.taupe}`,
        boxShadow: '0 2px 10px rgba(0,0,0,0.05)',
      }}
    >
      {statItems.map((item, index) => (
        <Box
          key={index}
          style={{
            textAlign: 'center',
          }}
        >
          <Box
            style={{
              fontFamily: checkInTheme.fonts.display,
              fontSize: '48px',
              fontWeight: 700,
              color: checkInTheme.colors.burgundy,
              marginBottom: checkInTheme.spacing.xs,
              lineHeight: 1,
            }}
          >
            {item.value}
          </Box>
          <Box
            style={{
              fontFamily: checkInTheme.fonts.heading,
              fontSize: '14px',
              color: checkInTheme.colors.stone,
              textTransform: 'uppercase',
              letterSpacing: '1px',
              fontWeight: 600,
            }}
          >
            {item.label}
          </Box>
        </Box>
      ))}
    </Box>
  )
}
