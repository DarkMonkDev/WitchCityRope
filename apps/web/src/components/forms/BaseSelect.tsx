import { forwardRef } from 'react';
import { Select, SelectProps, Loader } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';

export interface BaseSelectProps extends SelectProps {
  loading?: boolean;
  'data-testid'?: string;
}

export const BaseSelect = forwardRef<HTMLInputElement, BaseSelectProps>(
  ({ 
    loading = false,
    'data-testid': testId,
    rightSection,
    ...props 
  }, ref) => {
    // Determine right section content based on state
    const getRightSection = () => {
      if (loading) {
        return <Loader size="xs" />;
      }
      if (props.error) {
        return <IconAlertCircle size={16} color="var(--mantine-color-red-6)" />;
      }
      return rightSection;
    };

    return (
      <Select
        ref={ref}
        data-testid={testId}
        rightSection={getRightSection()}
        rightSectionProps={{ style: { pointerEvents: 'none' } }}
        {...props}
      />
    );
  }
);

BaseSelect.displayName = 'BaseSelect';