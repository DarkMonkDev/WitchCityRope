import React from 'react';
import { Paper, Stack, Title, Text, Button } from '@mantine/core';

interface VolunteerEncouragementBoxProps {
  onScrollToVolunteers: () => void;
}

/**
 * Volunteer Encouragement Box
 * Displays on event detail page to encourage users to volunteer
 * Shows only when:
 * - User is logged in
 * - User has NOT already volunteered
 * - Event has volunteer positions available
 * - NOT (event is full AND user doesn't have RSVP/ticket)
 */
export const VolunteerEncouragementBox: React.FC<VolunteerEncouragementBoxProps> = ({
  onScrollToVolunteers
}) => {
  return (
    <Paper
      style={{
        background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
        borderRadius: '16px',
        padding: 'var(--space-lg)',
        boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        border: '1px solid rgba(255,255,255,0.1)',
        position: 'relative',
        overflow: 'hidden'
      }}
    >
      {/* Subtle overlay pattern */}
      <div
        style={{
          position: 'absolute',
          top: '-50%',
          left: '-50%',
          width: '200%',
          height: '200%',
          background: 'radial-gradient(circle, rgba(255,255,255,0.05) 0%, transparent 70%)',
          transform: 'rotate(45deg)',
          pointerEvents: 'none'
        }}
      />

      <Stack gap="md" style={{ position: 'relative', zIndex: 1 }}>
        {/* Title */}
        <Title
          order={3}
          style={{
            fontFamily: 'var(--font-heading)',
            fontSize: '18px',
            fontWeight: 700,
            color: 'var(--color-ivory)',
            lineHeight: 1.3
          }}
        >
          Help Make This Event Great!
        </Title>

        {/* Description */}
        <Text
          size="sm"
          style={{
            color: 'var(--color-dusty-rose)',
            lineHeight: 1.6
          }}
        >
          Volunteer positions are available. Your help makes our community events possible.
        </Text>

        {/* Volunteer Button */}
        <Button
          fullWidth
          size="md"
          onClick={onScrollToVolunteers}
          styles={{
            root: {
              background: 'var(--color-ivory)',
              color: 'var(--color-burgundy)',
              fontWeight: 700,
              height: '44px',
              paddingTop: '12px',
              paddingBottom: '12px',
              fontSize: '14px',
              lineHeight: '1.2',
              border: 'none',
              transition: 'all 0.2s ease',
              '&:hover': {
                background: 'var(--color-cream)',
                transform: 'translateY(-2px)',
                boxShadow: '0 4px 8px rgba(0,0,0,0.15)'
              }
            }
          }}
        >
          View Volunteer Spots
        </Button>
      </Stack>
    </Paper>
  );
};
