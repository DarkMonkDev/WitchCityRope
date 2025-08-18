import { forwardRef } from 'react';
import { Textarea, TextareaProps, Loader } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';

export interface BaseTextareaProps extends TextareaProps {
  loading?: boolean;
  'data-testid'?: string;
}

export const BaseTextarea = forwardRef<HTMLTextAreaElement, BaseTextareaProps>(
  ({ 
    loading = false,
    'data-testid': testId,
    rightSection,
    autosize = true,
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
      <Textarea
        ref={ref}
        data-testid={testId}
        autosize={autosize}
        rightSection={getRightSection()}
        rightSectionProps={{ style: { pointerEvents: 'none' } }}
        {...props}
      />
    );
  }
);

BaseTextarea.displayName = 'BaseTextarea';