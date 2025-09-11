import React from 'react';
import { Group, Text, Progress } from '@mantine/core';

interface CapacityInfo {
  total: number;
  taken: number;
  available: number;
}

interface CapacityIndicatorProps {
  capacity: CapacityInfo;
  size?: 'sm' | 'md' | 'lg';
  showText?: boolean;
  warningThreshold?: number; // Default 0.8 (80%)
}

export const CapacityIndicator: React.FC<CapacityIndicatorProps> = ({ 
  capacity, 
  size = 'md', 
  showText = true,
  warningThreshold = 0.8 
}) => {
  const percentage = capacity.taken / capacity.total;
  
  const getCapacityColor = (): string => {
    if (percentage >= 1) return 'red';
    if (percentage >= warningThreshold) return 'red';
    if (percentage >= 0.6) return 'yellow';
    return 'burgundy';
  };

  const getCapacityText = (): string => {
    if (capacity.available === 0) return 'Full - Join Waitlist';
    if (percentage >= warningThreshold) return `Only ${capacity.available} spots left!`;
    return `${capacity.available} of ${capacity.total} spots available`;
  };

  const getProgressWidth = (): number => {
    switch (size) {
      case 'lg': return 120;
      case 'sm': return 60;
      default: return 80;
    }
  };

  return (
    <Group gap="xs" align="center">
      {showText && (
        <Text 
          size={size === 'lg' ? 'md' : 'sm'}
          c={percentage >= warningThreshold ? 'red' : 'dimmed'}
          fw={percentage >= warningThreshold ? 600 : 400}
        >
          {getCapacityText()}
        </Text>
      )}
      
      <Progress
        value={percentage * 100}
        color={getCapacityColor()}
        size={size}
        w={getProgressWidth()}
        aria-label={`Event capacity: ${capacity.taken} of ${capacity.total} spots taken`}
        style={{
          '& .mantine-Progress-bar': {
            transition: 'width 300ms ease, background-color 300ms ease'
          }
        }}
      />
    </Group>
  );
};