import React, { useState, useEffect } from 'react'
import { Box, Button } from '@mantine/core'
import { checkInTheme } from '../styles/theme'

interface CheckInHeaderProps {
  eventTitle: string
  eventDate: Date
  onExit: () => void
  checkedInCount: number
  totalCount: number
}

/**
 * CheckInHeader Component
 * Source: /docs/design/wireframes/event-checkin-visual.html (lines 524-537)
 *
 * Displays event info with countdown timer and exit button
 * Matches wireframe gradient background and typography exactly
 */
export const CheckInHeader: React.FC<CheckInHeaderProps> = ({
  eventTitle,
  eventDate,
  onExit,
  checkedInCount,
  totalCount,
}) => {
  const [timeLeft, setTimeLeft] = useState<string>('0:00:00')

  useEffect(() => {
    const updateCountdown = () => {
      const now = new Date()
      const diff = eventDate.getTime() - now.getTime()

      if (diff > 0) {
        const totalHours = Math.floor(diff / (1000 * 60 * 60))
        const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
        const seconds = Math.floor((diff % (1000 * 60)) / 1000)

        // Show days if >= 24 hours, otherwise just show hours
        if (totalHours >= 24) {
          const days = Math.floor(totalHours / 24)
          const hours = totalHours % 24
          setTimeLeft(
            `${days}d ${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
          )
        } else {
          setTimeLeft(
            `${totalHours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
          )
        }
      } else {
        setTimeLeft('00:00:00')
      }
    }

    updateCountdown()
    const interval = setInterval(updateCountdown, 1000)

    return () => clearInterval(interval)
  }, [eventDate])

  const formattedDate = eventDate.toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  })

  const formattedTime = eventDate.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })

  return (
    <Box
      style={{
        background: checkInTheme.gradients.header,
        color: checkInTheme.colors.ivory,
        padding: `${checkInTheme.spacing.lg} ${checkInTheme.spacing.xl}`,
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        boxShadow: '0 4px 20px rgba(0,0,0,0.15)',
      }}
    >
      {/* Left Section: Event Title and Date */}
      <Box style={{ flex: '1' }}>
        <Box
          component="h1"
          style={{
            fontFamily: checkInTheme.fonts.heading,
            fontSize: '32px',
            fontWeight: 800,
            letterSpacing: '-0.5px',
            margin: 0,
            marginBottom: checkInTheme.spacing.xs,
          }}
        >
          {eventTitle}
        </Box>
        <Box
          style={{
            fontSize: '16px',
            opacity: 0.9,
            fontWeight: 500,
          }}
        >
          {formattedDate} • {formattedTime}
        </Box>
      </Box>

      {/* Center Section: Check-in Count */}
      <Box
        style={{
          flex: '1',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
        }}
      >
        <Box
          style={{
            fontFamily: checkInTheme.fonts.heading,
            fontSize: '32px',
            fontWeight: 800,
            letterSpacing: '-0.5px',
            color: checkInTheme.colors.ivory,
            textAlign: 'center',
          }}
        >
          {checkedInCount} of {totalCount} Checked In
        </Box>
      </Box>

      {/* Right Section: Timer and Exit Button */}
      <Box
        style={{
          flex: '1',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'flex-end',
          gap: checkInTheme.spacing.md,
        }}
      >
        <Box
          style={{
            background: 'rgba(255,255,255,0.15)',
            backdropFilter: 'blur(10px)',
            padding: `${checkInTheme.spacing.sm} ${checkInTheme.spacing.md}`,
            borderRadius: '8px',
            fontWeight: 600,
            fontFamily: checkInTheme.fonts.heading,
            letterSpacing: '0.5px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
          }}
        >
          <Box
            style={{
              fontSize: '12px',
              textTransform: 'uppercase',
              opacity: 0.8,
              letterSpacing: '1px',
            }}
          >
            Time Until Start
          </Box>
          <Box
            style={{
              fontSize: '24px',
              fontWeight: 700,
              fontFamily: 'ui-monospace, "Cascadia Code", "Source Code Pro", Menlo, Consolas, "DejaVu Sans Mono", monospace',
              fontVariantNumeric: 'tabular-nums',
              letterSpacing: '0.5px',
            }}
          >
            {timeLeft}
          </Box>
        </Box>

        <Button
          onClick={() => (window.location.href = window.location.origin)}
          styles={{
            root: {
              background: 'transparent',
              border: `2px solid ${checkInTheme.colors.ivory}`,
              color: checkInTheme.colors.ivory,
              padding: '12px 16px',
              borderRadius: '8px',
              fontFamily: checkInTheme.fonts.heading,
              fontWeight: 700,
              fontSize: '32px',
              lineHeight: '1',
              height: 'auto',
              minWidth: '56px',
              transition: 'all 0.3s ease',
              '&:hover': {
                background: checkInTheme.colors.ivory,
                color: checkInTheme.colors.burgundy,
                transform: 'translateY(-2px)',
                boxShadow: '0 4px 12px rgba(0,0,0,0.2)',
              },
            },
          }}
        >
          ✕
        </Button>
      </Box>
    </Box>
  )
}
