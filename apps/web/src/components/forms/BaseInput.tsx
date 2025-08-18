import { forwardRef } from 'react';
import { TextInput, TextInputProps, Loader } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';

export interface BaseInputProps extends TextInputProps {
  loading?: boolean;
  asyncValidation?: boolean;
  'data-testid'?: string;
}

export const BaseInput = forwardRef<HTMLInputElement, BaseInputProps>(
  ({ 
    loading = false,
    asyncValidation = false,
    'data-testid': testId,
    rightSection,
    ...props 
  }, ref) => {
    // Determine right section content based on state
    const getRightSection = () => {
      if (loading || asyncValidation) {
        return <Loader size="xs" />;
      }
      if (props.error) {
        return <IconAlertCircle size={16} color="var(--mantine-color-red-6)" />;
      }
      return rightSection;
    };

    return (
      <TextInput
        ref={ref}
        data-testid={testId}
        rightSection={getRightSection()}
        rightSectionProps={{ style: { pointerEvents: 'none' } }}
        {...props}
      />
    );
  }
);

BaseInput.displayName = 'BaseInput';