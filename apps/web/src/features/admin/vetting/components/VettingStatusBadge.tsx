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
          backgroundColor: '#4169E1',
          color: 'white',
          label: 'New'
        };
      case 'inreview':
      case 'in review':
        return {
          backgroundColor: '#8B8680',
          color: 'white',
          label: 'Under Review'
        };
      case 'pendingreferences':
      case 'pending references':
        return {
          backgroundColor: '#DAA520',
          color: 'white',
          label: 'Pending References'
        };
      case 'interviewscheduled':
      case 'interview scheduled':
        return {
          backgroundColor: '#228B22',
          color: 'white',
          label: 'Interview Approved'
        };
      case 'approved':
        return {
          backgroundColor: '#228B22',
          color: 'white',
          label: 'Approved'
        };
      case 'rejected':
        return {
          backgroundColor: '#DC143C',
          color: 'white',
          label: 'Rejected'
        };
      case 'withdrawn':
        return {
          backgroundColor: '#8B8680',
          color: 'white',
          label: 'Withdrawn'
        };
      case 'on hold':
      case 'onhold':
        return {
          backgroundColor: '#8B8680',
          color: 'white',
          label: 'On Hold'
        };
      default:
        return {
          backgroundColor: '#8B8680',
          color: 'white',
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
      size={size}
      className={getStatusCssClass(status)}
      style={{
        backgroundColor: config.backgroundColor,
        color: config.color,
        borderRadius: '12px',
        fontWeight: 600,
        fontSize: '12px',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        border: 'none'
      }}
    >
      {config.label}
    </Badge>
  );
};