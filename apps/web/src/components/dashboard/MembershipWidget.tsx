import React from 'react';
import { Box, Text, Progress, Group, Loader, Alert, Badge, Stack } from '@mantine/core';
import { DashboardCard } from './DashboardCard';
import { useCurrentUser } from '../../features/auth/api/queries';
import { useNavigate } from 'react-router-dom';

// Mock role data - replace with actual role system integration
interface MembershipInfo {
  currentRole: 'Guest' | 'General Member' | 'Vetted Member' | 'Teacher' | 'Admin';
  memberSince?: string;
  vettingStatus?: 'not_eligible' | 'eligible' | 'pending' | 'approved' | 'rejected';
  vettingProgress?: number;
  benefits: string[];
  nextLevel?: {
    name: string;
    requirements: string[];
  };
}

/**
 * Membership Status Widget for Dashboard
 * Shows current membership level, vetting status, and progression path
 */
export const MembershipWidget: React.FC = () => {
  const { data: user, isLoading, error } = useCurrentUser();
  const navigate = useNavigate();

  // Mock membership data - replace with actual API integration
  const getMembershipInfo = (): MembershipInfo => {
    // This would come from the user's roles/membership data
    return {
      currentRole: 'General Member', // Would come from user.roles or similar
      memberSince: user?.createdAt,
      vettingStatus: 'eligible',
      vettingProgress: 65, // Based on profile completion + other requirements
      benefits: [
        'Access to public events',
        'Community forum participation', 
        'Member directory listing',
      ],
      nextLevel: {
        name: 'Vetted Member',
        requirements: [
          'Complete profile (80%+)',
          '30+ days membership',
          'Attend 2+ events',
          'Reference verification',
        ],
      },
    };
  };

  const membershipInfo = getMembershipInfo();
  
  const handleManageMembership = () => {
    navigate('/dashboard/membership');
  };

  const getRoleColor = (role: string) => {
    switch (role) {
      case 'Guest': return '#8B8680';
      case 'General Member': return '#DAA520';
      case 'Vetted Member': return '#228B22';
      case 'Teacher': return '#9D4EDD';
      case 'Admin': return '#DC143C';
      default: return '#8B8680';
    }
  };

  const getVettingStatusInfo = () => {
    switch (membershipInfo.vettingStatus) {
      case 'not_eligible':
        return { text: 'Not Eligible', color: '#8B8680', description: 'Complete requirements to apply' };
      case 'eligible':
        return { text: 'Ready to Apply', color: '#DAA520', description: 'You can now apply for vetting' };
      case 'pending':
        return { text: 'Under Review', color: '#9D4EDD', description: 'Application is being reviewed' };
      case 'approved':
        return { text: 'Approved', color: '#228B22', description: 'Congratulations! You are vetted' };
      case 'rejected':
        return { text: 'Needs Revision', color: '#DC143C', description: 'Please review feedback and reapply' };
      default:
        return { text: 'Unknown', color: '#8B8680', description: '' };
    }
  };

  const vettingStatus = getVettingStatusInfo();

  if (isLoading) {
    return (
      <DashboardCard
        title="Membership Status"
        icon="🎯"
        loading={true}
      >
        <Box style={{ textAlign: 'center', padding: '20px 0' }}>
          <Loader size="md" color="#880124" />
          <Text size="sm" c="dimmed" mt="sm">
            Loading membership...
          </Text>
        </Box>
      </DashboardCard>
    );
  }

  if (error) {
    return (
      <DashboardCard
        title="Membership Status"
        icon="🎯"
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
          Failed to load membership info
        </Alert>
      </DashboardCard>
    );
  }

  return (
    <DashboardCard
      title="Membership Status"
      icon="🎯"
      status={membershipInfo.currentRole}
      statusColor={getRoleColor(membershipInfo.currentRole)}
      action="Manage Membership"
      onActionClick={handleManageMembership}
    >
      <Stack gap="sm">
        {/* Current Role Info */}
        <Group justify="space-between" align="center">
          <Text
            size="sm"
            fw={600}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              color: '#2B2B2B',
            }}
          >
            Current Level
          </Text>
          <Badge
            size="lg"
            style={{
              backgroundColor: `${getRoleColor(membershipInfo.currentRole)}20`,
              color: getRoleColor(membershipInfo.currentRole),
              border: `1px solid ${getRoleColor(membershipInfo.currentRole)}40`,
              fontWeight: 600,
            }}
          >
            {membershipInfo.currentRole}
          </Badge>
        </Group>

        {/* Member Since */}
        {membershipInfo.memberSince && (
          <Group justify="space-between">
            <Text
              size="xs"
              c="dimmed"
              style={{ color: '#8B8680' }}
            >
              Member since:
            </Text>
            <Text
              size="xs"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontWeight: 500,
                color: '#4A4A4A',
              }}
            >
              {new Date(membershipInfo.memberSince).toLocaleDateString('en-US', {
                month: 'short',
                year: 'numeric'
              })}
            </Text>
          </Group>
        )}

        {/* Vetting Progress (if applicable) */}
        {membershipInfo.nextLevel && membershipInfo.vettingStatus !== 'approved' && (
          <Box>
            <Group justify="space-between" mb={4}>
              <Text
                size="xs"
                style={{
                  fontFamily: "'Montserrat', sans-serif",
                  fontWeight: 500,
                  color: '#4A4A4A',
                }}
              >
                Progress to {membershipInfo.nextLevel.name}
              </Text>
              <Text
                size="xs"
                style={{
                  fontFamily: "'Montserrat', sans-serif",
                  fontWeight: 600,
                  color: vettingStatus.color,
                }}
              >
                {membershipInfo.vettingProgress}%
              </Text>
            </Group>
            
            <Progress
              value={membershipInfo.vettingProgress || 0}
              size="sm"
              radius="md"
              style={{
                '& .mantine-Progress-bar': {
                  background: `linear-gradient(90deg, ${vettingStatus.color}, ${vettingStatus.color}CC)`,
                },
              }}
              mb="xs"
            />

            <Box
              style={{
                padding: '8px 10px',
                background: `${vettingStatus.color}15`,
                border: `1px solid ${vettingStatus.color}30`,
                borderRadius: '4px',
              }}
            >
              <Text
                size="xs"
                fw={600}
                style={{ color: vettingStatus.color, marginBottom: '2px' }}
              >
                {vettingStatus.text}
              </Text>
              <Text
                size="xs"
                style={{ color: '#8B8680', lineHeight: 1.3 }}
              >
                {vettingStatus.description}
              </Text>
            </Box>
          </Box>
        )}

        {/* Current Benefits */}
        <Box>
          <Text
            size="xs"
            fw={600}
            mb={4}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              color: '#4A4A4A',
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            Your Benefits
          </Text>
          <Stack gap={2}>
            {membershipInfo.benefits.slice(0, 3).map((benefit, index) => (
              <Text
                key={index}
                size="xs"
                style={{
                  color: '#8B8680',
                  paddingLeft: '8px',
                  position: 'relative',
                  lineHeight: 1.3,
                }}
              >
                <span style={{ 
                  position: 'absolute', 
                  left: 0, 
                  top: 0, 
                  color: '#228B22',
                  fontWeight: 'bold' 
                }}>
                  ✓
                </span>
                {benefit}
              </Text>
            ))}
            {membershipInfo.benefits.length > 3 && (
              <Text
                size="xs"
                c="dimmed"
                style={{ paddingLeft: '8px', fontStyle: 'italic' }}
              >
                +{membershipInfo.benefits.length - 3} more benefits
              </Text>
            )}
          </Stack>
        </Box>

        {/* Next Level Info (if eligible for vetting) */}
        {membershipInfo.vettingStatus === 'eligible' && (
          <Box
            mt="xs"
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
                fontWeight: 600,
                lineHeight: 1.3,
              }}
            >
              🎉 Ready for vetting! Apply to become a {membershipInfo.nextLevel.name}
            </Text>
          </Box>
        )}
      </Stack>
    </DashboardCard>
  );
};