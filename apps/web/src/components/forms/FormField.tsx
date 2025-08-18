import React from 'react';
import { Stack, StackProps } from '@mantine/core';

export interface FormFieldProps extends StackProps {
  children: React.ReactNode;
  // Note: With Mantine, labels, errors, and helper text are built into the input components
  // This wrapper is mainly for consistent spacing and grouping
}

export const FormField: React.FC<FormFieldProps> = ({ 
  children, 
  gap = 'xs',
  ...props 
}) => {
  return (
    <Stack gap={gap} {...props}>
      {children}
    </Stack>
  );
};

FormField.displayName = 'FormField';