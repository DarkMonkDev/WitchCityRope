import React from 'react';
import { Badge } from '@mantine/core';

type IncidentStatus = 'ReportSubmitted' | 'InformationGathering' | 'ReviewingFinalReport' | 'OnHold' | 'Closed';

interface IncidentStatusBadgeProps {
  status: IncidentStatus;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  'data-testid'?: string;
}

const statusConfig = {
  ReportSubmitted: {
    backgroundColor: '#614B79',
    color: 'white',
    label: 'Report Submitted',
    shortLabel: 'Submitted',
    ariaLabel: 'Report submitted, awaiting assignment'
  },
  InformationGathering: {
    backgroundColor: '#7B2CBF',
    color: 'white',
    label: 'Information Gathering',
    shortLabel: 'Investigating',
    ariaLabel: 'Information gathering in progress'
  },
  ReviewingFinalReport: {
    backgroundColor: '#E6AC00',
    color: 'white',
    label: 'Reviewing Final Report',
    shortLabel: 'Final Review',
    ariaLabel: 'Reviewing final report before closure'
  },
  OnHold: {
    backgroundColor: '#FFBF00',
    color: '#1A1A2E',
    label: 'On Hold',
    shortLabel: 'On Hold',
    ariaLabel: 'Investigation on hold pending information'
  },
  Closed: {
    backgroundColor: '#4A5C3A',
    color: 'white',
    label: 'Closed',
    shortLabel: 'Closed',
    ariaLabel: 'Incident closed and archived'
  }
};

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

export const IncidentStatusBadge: React.FC<IncidentStatusBadgeProps> = ({
  status,
  size = 'sm',
  'data-testid': dataTestId
}) => {
  const config = statusConfig[status];

  // Use short label for xs and sm sizes
  const displayLabel = (size === 'xs' || size === 'sm')
    ? config.shortLabel
    : config.label;

  return (
    <Badge
      size={size}
      data-testid={dataTestId}
      aria-label={config.ariaLabel}
      className={`status-${status.toLowerCase()}`}
      style={{
        backgroundColor: config.backgroundColor,
        color: config.color,
        borderRadius: '12px',
        fontWeight: 600,
        fontSize: getFontSize(size),
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        border: 'none',
        padding: size === 'lg' ? '8px 16px' : size === 'xl' ? '10px 20px' : undefined
      }}
    >
      {displayLabel}
    </Badge>
  );
};
