import React from 'react';
import { Text, Alert } from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle, IconInfoCircle } from '@tabler/icons-react';

export interface ValidationMessageProps {
  message?: string;
  fieldId: string;
  type?: 'error' | 'warning' | 'info';
  className?: string;
}

export const ValidationMessage: React.FC<ValidationMessageProps> = ({ 
  message, 
  fieldId, 
  type = 'error',
  className = ''
}) => {
  if (!message) return null;

  const getIcon = () => {
    switch (type) {
      case 'warning':
        return <IconAlertTriangle size={16} />;
      case 'info':
        return <IconInfoCircle size={16} />;
      case 'error':
      default:
        return <IconAlertCircle size={16} />;
    }
  };

  const getColor = () => {
    switch (type) {
      case 'warning':
        return 'yellow';
      case 'info':
        return 'blue';
      case 'error':
      default:
        return 'red';
    }
  };

  // For simple text error messages (most common case)
  if (type === 'error') {
    return (
      <Text
        id={`${fieldId}-error`}
        size="sm"
        c="red.6"
        role="alert"
        aria-live="polite"
        className={className}
        data-testid={`${fieldId}-error`}
      >
        {message}
      </Text>
    );
  }

  // For warning and info messages that need more prominence
  return (
    <Alert
      id={`${fieldId}-${type}`}
      variant="light"
      color={getColor()}
      icon={getIcon()}
      role="alert"
      aria-live="polite"
      className={className}
      data-testid={`${fieldId}-${type}`}
    >
      {message}
    </Alert>
  );
};

ValidationMessage.displayName = 'ValidationMessage';