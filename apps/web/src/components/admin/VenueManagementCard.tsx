import React, { useState, useEffect } from 'react';
import {
  Box,
  Group,
  Title,
  Text,
  Select,
  TextInput,
  Textarea,
  Checkbox,
  Stack,
  Button,
} from '@mantine/core';
import { IconBuilding, IconCheck, IconAlertCircle } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { useForm } from '@mantine/form';
import { api } from '../../api/client';
import type { components } from '@witchcityrope/shared-types';

type VenueDto = components['schemas']['VenueDto'];
type CreateVenueRequest = components['schemas']['CreateVenueRequest'];
type UpdateVenueRequest = components['schemas']['UpdateVenueRequest'];

interface VenueFormValues {
  name: string;
  directions: string;
  notes: string;
  isActive: boolean;
}

/**
 * Venue Management Card Component
 * Allows administrators to create, edit, and activate/deactivate venues.
 * Features:
 * - Dropdown with venues and "Add New" option
 * - Create new venues with blank form
 * - Edit existing venues with pre-populated data
 * - Toggle venue active status with "Active Venue" checkbox (soft delete)
 * - Success/error notifications
 */
export const VenueManagementCard: React.FC = () => {
  const queryClient = useQueryClient();
  const [selectedVenueId, setSelectedVenueId] = useState<string | null>(null);

  const form = useForm<VenueFormValues>({
    initialValues: {
      name: '',
      directions: '',
      notes: '',
      isActive: true,
    },
    validate: {
      name: (value) => (!value?.trim() ? 'Venue name is required' : null),
    },
  });

  // Fetch all venues (including inactive)
  const { data: venues, isLoading } = useQuery<VenueDto[]>({
    queryKey: ['admin', 'venues'],
    queryFn: async () => {
      const response = await api.get<VenueDto[]>('/api/admin/venues');
      return response.data || [];
    },
  });

  // Create venue mutation
  const createMutation = useMutation({
    mutationFn: async (data: CreateVenueRequest) => {
      const response = await api.post<VenueDto>('/api/admin/venues', data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'venues'] });
      notifications.show({
        title: 'Success',
        message: 'Venue created successfully',
        color: 'green',
        icon: <IconCheck />,
      });
      form.reset();
      setSelectedVenueId(null); // Clear selection
    },
    onError: (error: any) => {
      notifications.show({
        title: 'Error',
        message: error.response?.data?.message || 'Failed to create venue',
        color: 'red',
        icon: <IconAlertCircle />,
      });
    },
  });

  // Update venue mutation
  const updateMutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateVenueRequest }) => {
      const response = await api.put<VenueDto>(`/api/admin/venues/${id}`, data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'venues'] });
      notifications.show({
        title: 'Success',
        message: 'Venue updated successfully',
        color: 'green',
        icon: <IconCheck />,
      });
    },
    onError: (error: any) => {
      notifications.show({
        title: 'Error',
        message: error.response?.data?.message || 'Failed to update venue',
        color: 'red',
        icon: <IconAlertCircle />,
      });
    },
  });

  // Build dropdown options
  const dropdownOptions = [
    { value: 'add-new', label: 'Add New' },
    ...(venues
      ?.filter((v) => v.isActive)
      .map((v) => ({ value: v.id!.toString(), label: v.name! })) || []),
    ...(venues
      ?.filter((v) => !v.isActive)
      .map((v) => ({
        value: v.id!.toString(),
        label: `${v.name} (Inactive)`,
        style: { color: 'var(--color-stone)' },
      })) || []),
  ];

  // Form visibility logic
  const showForm = selectedVenueId !== null && selectedVenueId !== '';
  const isCreateMode = selectedVenueId === 'add-new';
  const isEditMode = showForm && !isCreateMode;

  // Handle venue selection
  const handleVenueChange = (value: string | null) => {
    setSelectedVenueId(value);

    if (value === 'add-new') {
      // Blank form for creating new venue
      form.reset();
      form.setValues({ name: '', directions: '', notes: '', isActive: true });
    } else if (value && value !== '') {
      // Load existing venue data
      const venue = venues?.find((v) => v.id!.toString() === value);
      if (venue) {
        form.setValues({
          name: venue.name || '',
          directions: venue.directions || '',
          notes: venue.notes || '',
          isActive: venue.isActive ?? true,
        });
      }
    } else {
      // No selection - hide form
      form.reset();
    }
  };

  // Handle form submission
  const handleSubmit = () => {
    const validation = form.validate();
    if (validation.hasErrors) {
      return;
    }

    if (isCreateMode) {
      createMutation.mutate({
        name: form.values.name.trim(),
        directions: form.values.directions.trim() || null,
        notes: form.values.notes.trim() || null,
      });
    } else if (isEditMode && selectedVenueId) {
      updateMutation.mutate({
        id: parseInt(selectedVenueId),
        data: {
          name: form.values.name.trim(),
          directions: form.values.directions.trim() || null,
          notes: form.values.notes.trim() || null,
          isActive: form.values.isActive,
        },
      });
    }
  };

  return (
    <>
      <Box
        style={{
          background: 'var(--color-ivory)',
          borderRadius: '16px',
          boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
          overflow: 'hidden',
          height: '100%',
        }}
      >
        {/* Card Header */}
        <Box
          style={{
            background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
            padding: 'var(--space-lg) var(--space-xl)',
            borderBottom: '1px solid var(--color-taupe)',
          }}
        >
          <Group gap="sm">
            <IconBuilding size={24} color="var(--color-ivory)" />
            <Title
              order={3}
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '20px',
                fontWeight: 700,
                color: 'var(--color-ivory)',
              }}
            >
              Venue Management
            </Title>
          </Group>
        </Box>

        {/* Card Body */}
        <Box style={{ padding: 'var(--space-xl)' }}>
          <Stack gap="lg">
            {/* Venue Dropdown */}
            <Box>
              <Text
                component="label"
                style={{
                  display: 'block',
                  fontFamily: 'var(--font-heading)',
                  fontSize: '14px',
                  fontWeight: 600,
                  color: 'var(--color-smoke)',
                  marginBottom: 'var(--space-xs)',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                }}
              >
                Venue
              </Text>
              <Select
                data={dropdownOptions}
                value={selectedVenueId}
                onChange={handleVenueChange}
                placeholder="Select a venue"
                searchable
                styles={{
                  input: {
                    fontFamily: 'var(--font-body)',
                    fontSize: '16px',
                    border: '2px solid var(--color-taupe)',
                    borderRadius: '8px',
                    background: 'var(--color-ivory)',
                    color: 'var(--color-charcoal)',
                    padding: 'var(--space-sm) var(--space-md)',
                    '&:focus': {
                      borderColor: 'var(--color-burgundy)',
                      boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
                    },
                  },
                }}
              />
            </Box>

            {/* Conditional Form */}
            {showForm && (
              <Box
                style={{
                  background: 'rgba(255, 248, 240, 0.5)',
                  border: '1px solid var(--color-taupe)',
                  borderRadius: '12px',
                  padding: 'var(--space-lg)',
                  marginTop: 'var(--space-md)',
                }}
              >
                <Stack gap="md">
                  {/* Venue Name */}
                  <Box>
                    <Text
                      component="label"
                      style={{
                        display: 'block',
                        fontFamily: 'var(--font-heading)',
                        fontSize: '14px',
                        fontWeight: 600,
                        color: 'var(--color-smoke)',
                        marginBottom: 'var(--space-xs)',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      Venue Name *
                    </Text>
                    <TextInput
                      {...form.getInputProps('name')}
                      placeholder="Enter venue name"
                      maxLength={100}
                      styles={{
                        input: {
                          fontFamily: 'var(--font-body)',
                          fontSize: '16px',
                          border: '2px solid var(--color-taupe)',
                          borderRadius: '8px',
                          background: 'var(--color-ivory)',
                          color: 'var(--color-charcoal)',
                          padding: 'var(--space-sm) var(--space-md)',
                          '&:focus': {
                            borderColor: 'var(--color-burgundy)',
                            boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
                          },
                        },
                      }}
                    />
                  </Box>

                  {/* Directions */}
                  <Box>
                    <Text
                      component="label"
                      style={{
                        display: 'block',
                        fontFamily: 'var(--font-heading)',
                        fontSize: '14px',
                        fontWeight: 600,
                        color: 'var(--color-smoke)',
                        marginBottom: 'var(--space-xs)',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      Directions
                    </Text>
                    <Textarea
                      {...form.getInputProps('directions')}
                      placeholder="Enter directions to venue"
                      rows={4}
                      maxLength={500}
                      styles={{
                        input: {
                          fontFamily: 'var(--font-body)',
                          fontSize: '16px',
                          border: '2px solid var(--color-taupe)',
                          borderRadius: '8px',
                          background: 'var(--color-ivory)',
                          color: 'var(--color-charcoal)',
                          padding: 'var(--space-sm) var(--space-md)',
                          resize: 'vertical',
                          '&:focus': {
                            borderColor: 'var(--color-burgundy)',
                            boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
                          },
                        },
                      }}
                    />
                  </Box>

                  {/* Notes */}
                  <Box>
                    <Text
                      component="label"
                      style={{
                        display: 'block',
                        fontFamily: 'var(--font-heading)',
                        fontSize: '14px',
                        fontWeight: 600,
                        color: 'var(--color-smoke)',
                        marginBottom: 'var(--space-xs)',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                      }}
                    >
                      Notes
                    </Text>
                    <Textarea
                      {...form.getInputProps('notes')}
                      placeholder="Enter additional notes"
                      rows={4}
                      maxLength={1000}
                      styles={{
                        input: {
                          fontFamily: 'var(--font-body)',
                          fontSize: '16px',
                          border: '2px solid var(--color-taupe)',
                          borderRadius: '8px',
                          background: 'var(--color-ivory)',
                          color: 'var(--color-charcoal)',
                          padding: 'var(--space-sm) var(--space-md)',
                          resize: 'vertical',
                          '&:focus': {
                            borderColor: 'var(--color-burgundy)',
                            boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
                          },
                        },
                      }}
                    />
                  </Box>

                  {/* Active Checkbox - Only shown when editing. Unchecking deactivates the venue (soft delete). */}
                  {isEditMode && (
                    <Checkbox
                      {...form.getInputProps('isActive', { type: 'checkbox' })}
                      label="Active Venue"
                    />
                  )}

                  {/* Action Buttons */}
                  <Group justify="flex-end" mt="md" gap="sm">
                    <Button
                      variant="filled"
                      color="#880124"
                      onClick={handleSubmit}
                      disabled={
                        !form.isValid() ||
                        createMutation.isPending ||
                        updateMutation.isPending
                      }
                      loading={createMutation.isPending || updateMutation.isPending}
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
                      {isCreateMode ? 'Create Venue' : 'Update Venue'}
                    </Button>
                  </Group>
                </Stack>
              </Box>
            )}
          </Stack>
        </Box>
      </Box>
    </>
  );
};
