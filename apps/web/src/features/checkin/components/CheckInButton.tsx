// CheckInButton - Multi-state button for streamlined check-in workflow
// Implements 3-step process: covidTest → checkIn → complete
// Payment handling moved to separate "Door Payment" column in CheckInInterface
// Source: /docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/design/ui-specifications.md

import React from 'react';
import { Button, Text } from '@mantine/core';
import { IconCheck } from '@tabler/icons-react';

export type CheckInButtonState = 'covidTest' | 'checkIn' | 'complete';

export interface CheckInButtonProps {
  attendee: {
    id: string;
    name: string;
    pronouns?: string;
    paymentStatus: 'ticket' | 'rsvp' | 'paidAtDoor';
    isCheckedIn: boolean;
  };
  currentState: CheckInButtonState;
  onStateChange: (newState: CheckInButtonState) => void;
  disabled?: boolean;
}

/**
 * State-driven check-in button with 3 states:
 * 1. covidTest - Electric purple gradient button
 * 2. checkIn - Green button for final check-in
 * 3. complete - Text display with checkmark
 */
export const CheckInButton: React.FC<CheckInButtonProps> = ({
  attendee,
  currentState,
  onStateChange,
  disabled = false
}) => {
  // Complete state - no button, just text
  if (currentState === 'complete') {
    return (
      <Text
        size="md"
        fw={600}
        c="green"
        style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
      >
        <IconCheck size={20} />
        Checked In
      </Text>
    );
  }

  // Covid Test button - electric purple gradient
  if (currentState === 'covidTest') {
    return (
      <Button
        onClick={() => onStateChange('checkIn')}
        disabled={disabled}
        aria-label={`Mark covid test complete for ${attendee.name}`}
        styles={{
          root: {
            background: 'linear-gradient(135deg, #9D4EDD 0%, #7B2CBF 100%)',
            color: '#FFF8F0',
            borderRadius: '12px 6px 12px 6px',
            fontFamily: 'Montserrat, sans-serif',
            fontWeight: 600,
            fontSize: '14px',
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
            transition: 'all 0.3s ease',
            height: '44px',
            paddingTop: '12px',
            paddingBottom: '12px',
            lineHeight: '1.2',
            boxShadow: '0 4px 15px rgba(157, 78, 221, 0.4)',
            '&:hover': {
              background: 'linear-gradient(135deg, #7B2CBF 0%, #9D4EDD 100%)',
              boxShadow: '0 6px 20px rgba(157, 78, 221, 0.5)',
              borderRadius: '6px 12px 6px 12px'
            }
          }
        }}
      >
        Covid Test Complete
      </Button>
    );
  }

  // Check In button - green
  if (currentState === 'checkIn') {
    return (
      <Button
        color="green"
        onClick={() => onStateChange('complete')}
        disabled={disabled}
        aria-label={`Check in ${attendee.name}`}
        styles={{
          root: {
            background: '#228B22',
            color: '#FFF8F0',
            borderRadius: '12px 6px 12px 6px',
            fontFamily: 'Montserrat, sans-serif',
            fontWeight: 600,
            fontSize: '14px',
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
            transition: 'all 0.3s ease',
            height: '44px',
            paddingTop: '12px',
            paddingBottom: '12px',
            lineHeight: '1.2',
            boxShadow: '0 4px 15px rgba(34, 139, 34, 0.4)',
            '&:hover': {
              background: '#1E7A1E',
              boxShadow: '0 6px 20px rgba(34, 139, 34, 0.5)',
              borderRadius: '6px 12px 6px 12px'
            }
          }
        }}
      >
        Check In
      </Button>
    );
  }

  return null;
};
