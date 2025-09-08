import React from 'react';
import { Box, Title, Text, Paper, TextInput, Group, Stack, Alert, Loader } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useCurrentUser } from '../../features/auth/api/queries';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';
import { ProfileForm } from '../../components/profile/ProfileForm';
import type { UserDto } from '@witchcityrope/shared-types';

/**
 * Profile Page - User profile management with API integration
 * Uses real API data via TanStack Query hooks
 */
export const ProfilePage: React.FC = () => {
  const { data: user, isLoading, error } = useCurrentUser();

  // Form for profile updates - prepopulated with user data
  const profileForm = useForm<Partial<UserDto>>({
    initialValues: {
      sceneName: user?.sceneName || '',
      email: user?.email || '',
      // Add other user fields as they become available from the API
    },
    validate: {
      sceneName: (value) => {
        if (!value || value.length < 2) return 'Scene name must be at least 2 characters';
        if (value.length > 50) return 'Scene name must be less than 50 characters';
        return null;
      },
      email: (value) => {
        if (!value) return 'Email is required';
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return 'Invalid email format';
        return null;
      },
    },
  });

  // Update form when user data loads
  React.useEffect(() => {
    if (user) {
      profileForm.setValues({
        sceneName: user.sceneName || '',
        email: user.email || '',
      });
    }
  }, [user]);

  const handleProfileSubmit = (values: Partial<UserDto>) => {
    // TODO: Implement profile update API call using mutation
    console.log('Profile update submitted:', values);
    // For now, just show the data that would be submitted
  };

  if (isLoading) {
    return (
      <DashboardLayout>
        <Box>
          <Title
            order={1}
            mb="xl"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '28px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Profile
          </Title>
          
          <Box style={{ textAlign: 'center', padding: '40px' }}>
            <Loader size="lg" color="#880124" />
            <Text style={{ marginTop: '16px', color: '#8B8680' }}>Loading your profile...</Text>
          </Box>
        </Box>
      </DashboardLayout>
    );
  }

  if (error) {
    return (
      <DashboardLayout>
        <Box>
          <Title
            order={1}
            mb="xl"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '28px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Profile
          </Title>
          
          <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
            Failed to load your profile. Please try refreshing the page.
          </Alert>
        </Box>
      </DashboardLayout>
    );
  }

  const [formError, setFormError] = React.useState<string | null>(null);
  
  const handleSubmit = (formData: any) => {
    // TODO: Implement profile update API call using mutation
    console.log('Profile update submitted:', formData);
    setFormError('Profile updates are not yet connected to the API. Form validation works, but changes won\'t be saved until the update mutation is implemented.');
  };

  return (
    <DashboardLayout>
      <Box>
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '28px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Profile Management
        </Title>

        <ProfileForm
          user={user}
          onSubmit={handleSubmit}
          isLoading={isLoading}
          error={formError}
        />

        {/* Account Information Section - Read Only */}
        <Paper
          mt="xl"
          style={{
            background: '#FFF8F0',
            padding: '24px',
            transition: 'all 0.3s ease',
            borderLeft: '3px solid #880124',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.boxShadow = '0 2px 12px rgba(0,0,0,0.05)';
            e.currentTarget.style.transform = 'translateX(2px)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.boxShadow = 'none';
            e.currentTarget.style.transform = 'translateX(0)';
          }}
        >
          <Title
            order={2}
            mb="lg"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '20px',
              fontWeight: 700,
              color: '#2B2B2B',
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            Account Information
          </Title>

          <Stack gap="md">
            <Box
              style={{
                background: 'rgba(183, 109, 117, 0.05)',
                padding: '16px',
                borderRadius: '6px',
                border: '1px solid rgba(183, 109, 117, 0.2)',
              }}
            >
              <Group>
                <Box style={{ flex: 1 }}>
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 600,
                      fontSize: '14px',
                      color: '#2B2B2B',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      marginBottom: '4px',
                    }}
                  >
                    User ID
                  </Text>
                  <Text
                    style={{
                      fontFamily: 'monospace',
                      fontSize: '16px',
                      color: '#8B8680',
                    }}
                  >
                    {user?.id || 'Not available'}
                  </Text>
                </Box>

                <Box style={{ flex: 1 }}>
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 600,
                      fontSize: '14px',
                      color: '#2B2B2B',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      marginBottom: '4px',
                    }}
                  >
                    Account Created
                  </Text>
                  <Text
                    style={{
                      fontSize: '16px',
                      color: '#8B8680',
                    }}
                  >
                    {user?.createdAt ? new Date(user.createdAt).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric'
                    }) : 'Not available'}
                  </Text>
                </Box>
              </Group>
            </Box>

            {user?.lastLoginAt && (
              <Box
                style={{
                  background: 'rgba(183, 109, 117, 0.05)',
                  padding: '16px',
                  borderRadius: '6px',
                  border: '1px solid rgba(183, 109, 117, 0.2)',
                }}
              >
                <Text
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontWeight: 600,
                    fontSize: '14px',
                    color: '#2B2B2B',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                    marginBottom: '4px',
                  }}
                >
                  Last Login
                </Text>
                <Text
                  style={{
                    fontSize: '16px',
                    color: '#8B8680',
                  }}
                >
                  {new Date(user.lastLoginAt!).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                  })}
                </Text>
              </Box>
            )}
          </Stack>
        </Paper>
      </Box>
    </DashboardLayout>
  );
};