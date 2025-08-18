import React from 'react';
import { Stack, Title, Divider, Paper } from '@mantine/core';
import { UseFormReturnType } from '@mantine/form';
import { BaseInput } from './BaseInput';
import { BaseSelect } from './BaseSelect';
import { PhoneInput } from './PhoneInput';

export interface EmergencyContactData {
  name: string;
  phone: string;
  relationship: string;
}

export interface EmergencyContactGroupProps {
  form: UseFormReturnType<any>;
  basePath?: string;
  relationshipOptions?: { value: string; label: string }[];
  required?: boolean;
  disabled?: boolean;
  'data-testid'?: string;
}

export const EmergencyContactGroup: React.FC<EmergencyContactGroupProps> = ({
  form,
  basePath = 'emergencyContact',
  relationshipOptions,
  required = true,
  disabled = false,
  'data-testid': testId = 'emergency-contact-group'
}) => {
  return (
    <Paper p="md" withBorder data-testid={testId}>
      <Stack gap="md">
        <div>
          <Title order={4} mb="xs">
            Emergency Contact Information
          </Title>
          <Divider />
        </div>
        
        <Stack gap="sm">
          <BaseInput
            label="Emergency contact name"
            placeholder="Jane Smith"
            required={required}
            disabled={disabled}
            {...form.getInputProps(`${basePath}.name`)}
            data-testid="emergency-contact-name"
          />
          
          <PhoneInput
            label="Emergency contact phone"
            placeholder="(555) 123-4567"
            required={required}
            disabled={disabled}
            {...form.getInputProps(`${basePath}.phone`)}
            data-testid="emergency-contact-phone"
          />
          
          {relationshipOptions ? (
            <BaseSelect
              label="Relationship to emergency contact"
              placeholder="Select relationship"
              data={relationshipOptions}
              required={required}
              disabled={disabled}
              {...form.getInputProps(`${basePath}.relationship`)}
              data-testid="emergency-contact-relationship"
            />
          ) : (
            <BaseInput
              label="Relationship to emergency contact"
              placeholder="Spouse, Partner, Friend, etc."
              required={required}
              disabled={disabled}
              {...form.getInputProps(`${basePath}.relationship`)}
              data-testid="emergency-contact-relationship"
            />
          )}
        </Stack>
      </Stack>
    </Paper>
  );
};

EmergencyContactGroup.displayName = 'EmergencyContactGroup';