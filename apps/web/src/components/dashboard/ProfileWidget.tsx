import React from 'react';
import { Box, Text, Progress, Group, Loader, Alert } from '@mantine/core';
import { DashboardCard } from './DashboardCard';
import { useCurrentUser } from '../../features/auth/api/queries';
import { useNavigate } from 'react-router-dom';

/**
 * Profile Widget for Dashboard
 * Shows user profile completion and basic info
 */
export const ProfileWidget: React.FC = () => {
  const { data: user, isLoading, error } = useCurrentUser();
  const navigate = useNavigate();

  // Calculate profile completion percentage
  const calculateProfileCompletion = () => {
    if (!user) return 0;
    
    let completed = 0;
    const requiredFields = 6; // Adjust based on actual required fields
    
    if (user.sceneName) completed++;
    if (user.email) completed++;
    if (user.pronouns) completed++;
    if (user.role) completed++;
    // Note: UserDto doesn't have firstName/lastName fields
    // Using available fields: sceneName, email, pronouns, role, isVetted
    
    return Math.round((completed / requiredFields) * 100);
  };

  const profileCompletion = calculateProfileCompletion();
  
  // Determine completion status and color
  const getCompletionStatus = () => {
    if (profileCompletion >= 90) return { status: 'Complete', color: '#228B22' };
    if (profileCompletion >= 70) return { status: 'Good', color: '#DAA520' };
    if (profileCompletion >= 50) return { status: 'Needs Work', color: '#FF8C00' };
    return { status: 'Incomplete', color: '#DC143C' };
  };

  const { status, color } = getCompletionStatus();

  const handleUpdateProfile = () => {
    navigate('/dashboard/profile');
  };

  if (isLoading) {
    return (
      <DashboardCard
        title="Profile Status"
        icon="👤"
        loading={true}
      >
        <Box style={{ textAlign: 'center', padding: '20px 0' }}>
          <Loader size="md" color="#880124" />
          <Text size="sm" c="dimmed" mt="sm">
            Loading profile...
          </Text>
        </Box>
      </DashboardCard>
    );
  }

  if (error) {
    return (
      <DashboardCard
        title="Profile Status"
        icon="👤"
        status="Error"
        statusColor="#DC143C"
      >
        <Alert 
          color="red" 
          variant="light"
          style={{ 
            background: 'rgba(220, 20, 60, 0.05)', 
            border: '1px solid rgba(220, 20, 60, 0.2)' 
          }}
        >
          Failed to load profile
        </Alert>
      </DashboardCard>
    );
  }

  return (
    <DashboardCard
      title="Profile Status"
      icon="👤"
      status={`${profileCompletion}% Complete`}
      statusColor={color}
      action="Update Profile"
      onActionClick={handleUpdateProfile}
    >
      <Box>
        {/* User Info */}
        <Group justify="space-between" mb="sm">
          <Text
            size="sm"
            fw={600}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              color: '#2B2B2B',
            }}
          >
            {user?.sceneName || 'Unknown User'}
          </Text>
          <Text
            size="xs"
            c="dimmed"
            style={{ color: '#8B8680' }}
          >
            Member ID: {user?.id?.slice(-6) || 'N/A'}
          </Text>
        </Group>

        {/* Profile Completion Bar */}
        <Box mb="sm">
          <Group justify="space-between" mb={4}>
            <Text
              size="xs"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontWeight: 500,
                color: '#4A4A4A',
              }}
            >
              Profile Completion
            </Text>
            <Text
              size="xs"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontWeight: 600,
                color: color,
              }}
            >
              {status}
            </Text>
          </Group>
          
          <Progress
            value={profileCompletion}
            size="sm"
            radius="md"
            style={{
              '& .mantine-Progress-bar': {
                background: `linear-gradient(90deg, ${color}, ${color}CC)`,
              },
            }}
          />
        </Box>

        {/* Member Since */}
        <Group justify="space-between" align="center" mt="sm">
          <Text
            size="xs"
            c="dimmed"
            style={{ color: '#8B8680' }}
          >
            Member since: {user?.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'N/A'}
          </Text>
          {user?.emailConfirmed && (
            <Box
              style={{
                width: '6px',
                height: '6px',
                borderRadius: '50%',
                background: '#228B22',
              }}
              title="Email Verified"
            />
          )}
        </Group>

        {/* Profile Tips */}
        {profileCompletion < 90 && (
          <Box
            mt="sm"
            style={{
              padding: '8px 10px',
              background: 'rgba(255, 191, 0, 0.1)',
              border: '1px solid rgba(255, 191, 0, 0.3)',
              borderRadius: '4px',
            }}
          >
            <Text
              size="xs"
              style={{
                color: '#B8860B',
                fontWeight: 500,
                lineHeight: 1.3,
              }}
            >
              💡 Complete your profile to unlock all community features
            </Text>
          </Box>
        )}
      </Box>
    </DashboardCard>
  );
};