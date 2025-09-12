import React from 'react';
import { Stack, Text, Progress, Box } from '@mantine/core';

interface CapacityDisplayProps {
  current?: number;
  max?: number;
}

export const CapacityDisplay: React.FC<CapacityDisplayProps> = ({ 
  current, 
  max 
}) => {
  // Handle missing capacity data from API
  if (max === 0 || (max === undefined && current === undefined)) {
    return (
      <Box ta="center">
        <Text fw={500} c="dimmed" size="md">
          Capacity TBD
        </Text>
      </Box>
    );
  }

  // Fallback values for undefined/null
  const currentValue = current ?? 0;
  const maxValue = max ?? 0;
  
  const percentage = maxValue > 0 ? (currentValue / maxValue) * 100 : 0;
  
  const getColor = () => {
    if (percentage >= 80) return 'green'; // High capacity = positive (nearly sold out!)
    if (percentage >= 50) return 'yellow'; // Moderate capacity = okay
    return 'red'; // Low capacity = concerning (needs more signups)
  };

  return (
    <Box ta="center">
      <Text fw={700} c="wcr.7" size="md" mb={2}>
        {currentValue}/{maxValue}
      </Text>
      <Box style={{ display: 'flex', justifyContent: 'center' }}>
        <Progress
          value={percentage}
          color={getColor()}
          size="sm"
          radius="xs"
          style={{ minWidth: '60px', maxWidth: '100px' }}
          // Accessibility attributes
          aria-label={`${currentValue} of ${maxValue} spots filled`}
          aria-valuenow={currentValue}
          aria-valuemin={0}
          aria-valuemax={maxValue}
        />
      </Box>
    </Box>
  );
};