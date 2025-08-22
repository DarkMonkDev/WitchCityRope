import React from 'react';
import { Box } from '@mantine/core';

interface RopeDividerProps {
  /** Custom height for the divider */
  height?: number;
  /** Custom colors for the rope paths */
  colors?: {
    primary?: string;
    secondary?: string;
  };
}

export const RopeDivider: React.FC<RopeDividerProps> = ({
  height = 80,
  colors = {
    primary: '#880124',
    secondary: '#B76D75'
  }
}) => {
  return (
    <Box
      style={{
        width: '100%',
        height: `${height}px`,
        margin: 'var(--space-xl) 0',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <svg
        viewBox="0 0 1200 100"
        preserveAspectRatio="none"
        style={{
          position: 'absolute',
          width: '100%',
          height: '100%',
        }}
      >
        <path
          d="M0,50 Q300,20 600,50 T1200,50"
          stroke={colors.primary}
          strokeWidth="3"
          fill="none"
          opacity="0.3"
        />
        <path
          d="M0,50 Q300,80 600,50 T1200,50"
          stroke={colors.secondary}
          strokeWidth="2"
          fill="none"
          opacity="0.4"
        />
      </svg>
    </Box>
  );
};