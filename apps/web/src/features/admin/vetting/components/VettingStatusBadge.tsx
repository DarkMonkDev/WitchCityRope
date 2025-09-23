import React from 'react';
import { Badge } from '@mantine/core';

interface VettingStatusBadgeProps {
  status: string;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  'data-testid'?: string;
}

export const VettingStatusBadge: React.FC<VettingStatusBadgeProps> = ({
  status,
  size = 'sm',
  'data-testid': dataTestId
}) => {
  const getStatusConfig = (status: string) => {
    switch (status.toLowerCase()) {
      case 'draft':
        return {
          backgroundColor: '#1c7ed6',
          color: 'white',
          label: 'Draft'
        };
      case 'underreview':
      case 'inreview':
      case 'in review':
        return {
          backgroundColor: '#868e96',
          color: 'white',
          label: 'Under Review'
        };
      case 'interviewapproved':
      case 'pendingreferences':
      case 'pending references':
        return {
          backgroundColor: '#D4AF37',
          color: 'white',
          label: 'Interview Approved'
        };
      case 'pendinginterview':
      case 'interviewscheduled':
      case 'interview scheduled':
        return {
          backgroundColor: '#1c7ed6',
          color: 'white',
          label: 'Pending Interview'
        };
      case 'approved':
        return {
          backgroundColor: '#51cf66',
          color: 'white',
          label: 'Approved'
        };
      case 'denied':
      case 'rejected':
        return {
          backgroundColor: '#c92a2a',
          color: 'white',
          label: 'Denied'
        };
      case 'withdrawn':
        return {
          backgroundColor: '#868e96',
          color: 'white',
          label: 'Withdrawn'
        };
      case 'on hold':
      case 'onhold':
        return {
          backgroundColor: '#fd7e14',
          color: 'white',
          label: 'On Hold'
        };
      default:
        return {
          backgroundColor: '#868e96',
          color: 'white',
          label: status
        };
    }
  };

  const config = getStatusConfig(status);

  // Get font size based on size prop
  const getFontSize = (size: string) => {
    switch (size) {
      case 'xs': return '10px';
      case 'sm': return '12px';
      case 'md': return '14px';
      case 'lg': return '16px';
      case 'xl': return '18px';
      default: return '12px';
    }
  };

  // Get CSS class for wireframe compatibility
  const getStatusCssClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'draft':
        return 'status-draft';
      case 'underreview':
      case 'inreview':
      case 'in review':
        return 'status-under-review';
      case 'interviewapproved':
      case 'pendingreferences':
      case 'pending references':
        return 'status-interview-approved';
      case 'pendinginterview':
      case 'interviewscheduled':
      case 'interview scheduled':
        return 'status-pending-interview';
      case 'approved':
        return 'status-approved';
      case 'denied':
      case 'rejected':
        return 'status-denied';
      case 'withdrawn':
        return 'status-withdrawn';
      case 'on hold':
      case 'onhold':
        return 'status-on-hold';
      default:
        return 'status-unknown';
    }
  };

  return (
    <Badge
      size={size}
      className={getStatusCssClass(status)}
      data-testid={dataTestId}
      style={{
        backgroundColor: config.backgroundColor,
        color: config.color,
        borderRadius: '12px',
        fontWeight: 600,
        fontSize: getFontSize(size),
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        border: 'none'
      }}
    >
      {config.label}
    </Badge>
  );
};