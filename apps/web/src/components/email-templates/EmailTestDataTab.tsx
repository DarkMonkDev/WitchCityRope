import React, { useState, useEffect } from 'react';
import {
  Stack,
  Paper,
  SimpleGrid,
  TextInput,
  Button,
  Text,
  Divider,
  Group,
  Box,
  Loader,
  Alert,
} from '@mantine/core';
import { IconAlertCircle, IconCheck } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { emailTemplatesApi } from '../../services/emailTemplates.api';

// Variable groups are fetched from the backend registry via API.
// No hardcoded variable lists — the registry is the single source of truth.

/**
 * Converts a snake_case string to Title Case.
 * e.g., "scene_name" -> "Scene Name"
 */
const toTitleCase = (snakeCase: string): string =>
  snakeCase
    .split('_')
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(' ');

/**
 * Email Test Data Tab - Manages all default variable values used when sending test emails.
 * Displays variables organized by category with editable text inputs.
 * Values persist in the database via the Settings table.
 */
export const EmailTestDataTab: React.FC = () => {
  const queryClient = useQueryClient();
  const [localValues, setLocalValues] = useState<Record<string, string>>({});

  // Fetch variable groups from the backend registry (single source of truth)
  const {
    data: variableGroups,
    isLoading: isLoadingVariables,
  } = useQuery<Record<string, string[]>>({
    queryKey: ['email-template-all-variables'],
    queryFn: () => emailTemplatesApi.getAllVariablesGrouped(),
  });

  // Fetch saved test data
  const {
    data: testData,
    isLoading: isLoadingTestData,
    error,
  } = useQuery<Record<string, string>>({
    queryKey: ['email-test-data'],
    queryFn: () => emailTemplatesApi.getTestData(),
  });

  const isLoading = isLoadingVariables || isLoadingTestData;

  // Populate local state when data loads
  useEffect(() => {
    if (testData) {
      setLocalValues(testData);
    }
  }, [testData]);

  // Save mutation
  const saveMutation = useMutation<void, Error, Record<string, string>>({
    mutationFn: (data: Record<string, string>) => emailTemplatesApi.saveTestData(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-test-data'] });
      notifications.show({
        message: 'Test data defaults saved successfully',
        color: 'green',
        icon: <IconCheck size={16} />,
      });
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to save test data',
        color: 'red',
      });
    },
  });

  const handleChange = (variable: string, value: string) => {
    setLocalValues((prev) => ({ ...prev, [variable]: value }));
  };

  const handleSave = () => {
    saveMutation.mutate(localValues);
  };

  // Loading state
  if (isLoading) {
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Loader size="lg" />
        <Text mt="md" c="dimmed">
          Loading test data...
        </Text>
      </Box>
    );
  }

  // Error state
  if (error) {
    return (
      <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Test Data">
        <Text>{(error as Error).message || 'Failed to load test data'}</Text>
      </Alert>
    );
  }

  return (
    <Paper shadow="sm" radius="md" p="xl" style={{ border: '1px solid rgba(136, 1, 36, 0.1)' }}>
      <Stack gap="md">
        <Text fw={600} c="burgundy" size="lg">
          Email Template Test Data
        </Text>
        <Text size="sm" c="dimmed" mb="lg">
          Default values used when sending test emails. Changes are saved and persist across
          sessions.
        </Text>

        {variableGroups && Object.entries(variableGroups).map(([categoryName, variables], index) => (
          <React.Fragment key={categoryName}>
            {index > 0 && <Divider my="md" />}
            <Text fw={600} c="burgundy" size="md" mb="xs">
              {categoryName}
            </Text>
            <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
              {variables.map((variable) => (
                <TextInput
                  key={variable}
                  label={toTitleCase(variable)}
                  value={localValues[variable] || ''}
                  onChange={(e) => handleChange(variable, e.currentTarget.value)}
                />
              ))}
            </SimpleGrid>
          </React.Fragment>
        ))}

        <Divider my="md" />

        <Group justify="flex-end">
          <Button
            onClick={handleSave}
            loading={saveMutation.isPending}
            styles={{
              root: {
                fontWeight: 600,
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2',
              },
            }}
          >
            Save Defaults
          </Button>
        </Group>
      </Stack>
    </Paper>
  );
};
