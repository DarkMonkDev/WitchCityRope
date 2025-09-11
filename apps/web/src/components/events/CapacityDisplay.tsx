import React from 'react';
import { Stack, Text, Progress } from '@mantine/core';

interface CapacityDisplayProps {
  current: number;
  max: number;
}

export const CapacityDisplay: React.FC<CapacityDisplayProps> = ({ 
  current, 
  max 
}) => {
  const percentage = max > 0 ? (current / max) * 100 : 0;
  
  const getColor = () => {
    if (percentage >= 80) return 'red';
    if (percentage >= 60) return 'yellow';
    return 'green';
  };

  return (
    <Stack gap="xs" align="center">
      <Text fw={700} c="wcr.7" size="sm">
        {current}/{max}
      </Text>
      <Progress
        value={percentage}
        color={getColor()}
        size="sm"
        radius="xs"
        w="100%"
        style={{ minWidth: '60px' }}
        // Accessibility attributes
        aria-label={`${current} of ${max} spots filled`}
        aria-valuenow={current}
        aria-valuemin={0}
        aria-valuemax={max}
      />
    </Stack>
  );
};