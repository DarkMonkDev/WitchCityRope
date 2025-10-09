import React from 'react';
import { Alert, Anchor, Box, Text } from '@mantine/core';
import type { VettingStatusDto } from '../../../types/dashboard.types';

interface VettingAlertBoxProps {
  status: VettingStatusDto;
}

/**
 * Conditional vetting status alert box for user dashboard
 * Shows different alerts based on vetting status
 * Only displays for non-Vetted users (Pending, ApprovedForInterview, OnHold, Denied)
 */
export const VettingAlertBox: React.FC<VettingAlertBoxProps> = ({ status }) => {
  // Don't render for Vetted users
  if (status.status === 'Vetted') {
    return null;
  }

  const alertConfigs = {
    Pending: {
      icon: '⏰',
      color: 'blue',
      title: 'Application Under Review',
      message:
        "Your membership application is currently under review. We'll notify you via email once it's been reviewed.",
    },
    ApprovedForInterview: {
      icon: '✅',
      color: 'green',
      title: 'Great News! Your Application Has Been Approved',
      message: status.interviewScheduleUrl ? (
        <>
          <Anchor href={status.interviewScheduleUrl} c="burgundy" fw={600} td="underline">
            Schedule your vetting interview here
          </Anchor>{' '}
          to complete your membership.
        </>
      ) : (
        'Please schedule your vetting interview to complete your membership.'
      ),
    },
    OnHold: {
      icon: '⏸️',
      color: 'yellow',
      title: 'Membership On Hold',
      message: "Your membership is currently on hold. Contact us if you'd like to resume your membership.",
    },
    Denied: {
      icon: '❌',
      color: 'red',
      title: 'Application Not Approved',
      message: status.reapplyInfoUrl ? (
        <>
          Your membership application was not approved at this time.{' '}
          <Anchor href={status.reapplyInfoUrl} c="burgundy" fw={600} td="underline">
            Learn about reapplying
          </Anchor>
          .
        </>
      ) : (
        'Your membership application was not approved at this time.'
      ),
    },
  };

  const config = alertConfigs[status.status];

  if (!config) {
    return null;
  }

  return (
    <Alert
      icon={
        <Box component="span" fz="24px">
          {config.icon}
        </Box>
      }
      color={config.color}
      title={config.title}
      radius="md"
      mb="lg"
      styles={{
        root: {
          borderWidth: '2px',
        },
      }}
    >
      <Text size="sm" style={{ lineHeight: 1.6 }}>
        {config.message}
      </Text>
    </Alert>
  );
};
