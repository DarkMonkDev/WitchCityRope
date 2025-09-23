import React from 'react';
import { Badge } from '@mantine/core';

interface VettingStatusBadgeProps {
  status: string;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
}

export const VettingStatusBadge: React.FC<VettingStatusBadgeProps> = ({
  status,
  size = 'sm'
}) => {
  const getStatusConfig = (status: string) => {
    switch (status.toLowerCase()) {
      case 'new':
        return {
          color: 'blue',
          label: 'New'
        };
      case 'inreview':
      case 'in review':
        return {
          color: 'yellow',
          label: 'In Review'
        };
      case 'pendingreferences':
      case 'pending references':
        return {
          color: 'orange',
          label: 'Pending References'
        };
      case 'interviewscheduled':
      case 'interview scheduled':
        return {
          color: 'purple',
          label: 'Interview Scheduled'
        };
      case 'approved':
        return {
          color: 'green',
          label: 'Approved'
        };
      case 'rejected':
        return {
          color: 'red',
          label: 'Rejected'
        };
      case 'withdrawn':
        return {
          color: 'gray',
          label: 'Withdrawn'
        };
      case 'on hold':
      case 'onhold':
        return {
          color: 'gray',
          label: 'On Hold'
        };
      default:
        return {
          color: 'gray',
          label: status
        };
    }
  };

  const config = getStatusConfig(status);

  // Get CSS class for wireframe compatibility
  const getStatusCssClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'new':
        return 'status-new';
      case 'inreview':
      case 'in review':
        return 'status-under-review';
      case 'pendingreferences':
      case 'pending references':
        return 'status-pending-references';
      case 'interviewscheduled':
      case 'interview scheduled':
        return 'status-interview-approved';
      case 'approved':
        return 'status-approved';
      case 'rejected':
        return 'status-rejected';
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
      color={config.color}
      variant="light"
      size={size}
      className={getStatusCssClass(status)}
    >
      {config.label}
    </Badge>
  );
};