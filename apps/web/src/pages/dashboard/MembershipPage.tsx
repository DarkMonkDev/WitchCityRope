import React from 'react';
import { Box, Title, Text, Paper, Badge, Stack, Alert, Loader, Progress, Group } from '@mantine/core';
import { useCurrentUser } from '../../features/auth/api/queries';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';

/**
 * Membership Page - Displays user's membership status and community standing
 * Uses real API data via TanStack Query hooks
 */
export const MembershipPage: React.FC = () => {
  const { data: user, isLoading, error } = useCurrentUser();

  // Mock membership data - this will be replaced with real API calls
  // TODO: Create membership API endpoint to get user's membership details
  const membershipStatus = 'Active'; // This would come from API
  const membershipLevel = 'General Member'; // This would come from API
  const joinDate = user?.createdAt ? new Date(user.createdAt) : new Date();
  const membershipDuration = Math.floor((new Date().getTime() - joinDate.getTime()) / (1000 * 60 * 60 * 24));

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
            Membership
          </Title>
          
          <Box style={{ textAlign: 'center', padding: '40px' }}>
            <Loader size="lg" color="#880124" />
            <Text style={{ marginTop: '16px', color: '#8B8680' }}>Loading your membership information...</Text>
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
            Membership
          </Title>
          
          <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
            Failed to load your membership information. Please try refreshing the page.
          </Alert>
        </Box>
      </DashboardLayout>
    );
  }

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
          Membership
        </Title>

        {/* Note about future functionality */}
        <Alert 
          color="blue" 
          mb="xl" 
          style={{ 
            background: 'rgba(59, 130, 246, 0.1)', 
            border: '1px solid rgba(59, 130, 246, 0.3)',
            borderRadius: '6px'
          }}
        >
          <Text style={{ fontSize: '14px' }}>
            <strong>Note:</strong> Membership data is currently using mock information. Future update will connect to a dedicated membership API endpoint.
          </Text>
        </Alert>

        <Stack gap="lg">
          {/* Membership Status Overview */}
          <Paper
            style={{
              background: '#FFF8F0',
              padding: '24px',
              borderLeft: '4px solid #228B22',
              transition: 'all 0.3s ease',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = 'translateY(-4px)';
              e.currentTarget.style.boxShadow = '0 4px 16px rgba(0,0,0,0.08)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = 'translateY(0)';
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            <Group justify="space-between" mb="lg">
              <Title
                order={2}
                style={{
                  fontFamily: "'Montserrat', sans-serif",
                  fontSize: '20px',
                  fontWeight: 700,
                  color: '#2B2B2B',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                }}
              >
                Membership Status
              </Title>

              <Badge
                size="lg"
                style={{
                  background: '#228B2220',
                  color: '#228B22',
                  border: '1px solid #228B22',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                  fontSize: '12px',
                  fontWeight: 700,
                }}
              >
                {membershipStatus}
              </Badge>
            </Group>

            <Group grow>
              <Box>
                <Text
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontWeight: 600,
                    fontSize: '14px',
                    color: '#2B2B2B',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                    marginBottom: '8px',
                  }}
                >
                  Member Level
                </Text>
                <Text
                  style={{
                    fontSize: '18px',
                    fontWeight: 600,
                    color: '#880124',
                  }}
                >
                  {membershipLevel}
                </Text>
              </Box>

              <Box>
                <Text
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontWeight: 600,
                    fontSize: '14px',
                    color: '#2B2B2B',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                    marginBottom: '8px',
                  }}
                >
                  Member Since
                </Text>
                <Text
                  style={{
                    fontSize: '18px',
                    fontWeight: 600,
                    color: '#880124',
                  }}
                >
                  {joinDate.toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                  })}
                </Text>
              </Box>

              <Box>
                <Text
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontWeight: 600,
                    fontSize: '14px',
                    color: '#2B2B2B',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                    marginBottom: '8px',
                  }}
                >
                  Days as Member
                </Text>
                <Text
                  style={{
                    fontSize: '18px',
                    fontWeight: 600,
                    color: '#880124',
                  }}
                >
                  {membershipDuration} days
                </Text>
              </Box>
            </Group>
          </Paper>

          {/* Member Benefits */}
          <Paper
            style={{
              background: '#FFF8F0',
              padding: '24px',
              transition: 'all 0.3s ease',
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
              Member Benefits
            </Title>

            <Stack gap="md">
              <Box
                style={{
                  background: 'rgba(183, 109, 117, 0.05)',
                  padding: '16px',
                  borderRadius: '6px',
                  border: '1px solid rgba(183, 109, 117, 0.2)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '16px',
                }}
              >
                <Box
                  style={{
                    width: '40px',
                    height: '40px',
                    background: 'linear-gradient(135deg, #880124 0%, #614B79 100%)',
                    borderRadius: '8px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: '18px',
                  }}
                >
                  📅
                </Box>
                <Box style={{ flex: 1 }}>
                  <Text
                    style={{
                      fontWeight: 600,
                      fontSize: '16px',
                      color: '#2B2B2B',
                      marginBottom: '4px',
                    }}
                  >
                    Event Registration
                  </Text>
                  <Text style={{ fontSize: '14px', color: '#8B8680' }}>
                    Priority access to register for workshops and community events
                  </Text>
                </Box>
                <Badge color="green" size="sm">Active</Badge>
              </Box>

              <Box
                style={{
                  background: 'rgba(183, 109, 117, 0.05)',
                  padding: '16px',
                  borderRadius: '6px',
                  border: '1px solid rgba(183, 109, 117, 0.2)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '16px',
                }}
              >
                <Box
                  style={{
                    width: '40px',
                    height: '40px',
                    background: 'linear-gradient(135deg, #880124 0%, #614B79 100%)',
                    borderRadius: '8px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: '18px',
                  }}
                >
                  👥
                </Box>
                <Box style={{ flex: 1 }}>
                  <Text
                    style={{
                      fontWeight: 600,
                      fontSize: '16px',
                      color: '#2B2B2B',
                      marginBottom: '4px',
                    }}
                  >
                    Community Access
                  </Text>
                  <Text style={{ fontSize: '14px', color: '#8B8680' }}>
                    Access to member-only discussions and resources
                  </Text>
                </Box>
                <Badge color="green" size="sm">Active</Badge>
              </Box>

              <Box
                style={{
                  background: 'rgba(183, 109, 117, 0.05)',
                  padding: '16px',
                  borderRadius: '6px',
                  border: '1px solid rgba(183, 109, 117, 0.2)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '16px',
                }}
              >
                <Box
                  style={{
                    width: '40px',
                    height: '40px',
                    background: 'linear-gradient(135deg, #880124 0%, #614B79 100%)',
                    borderRadius: '8px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: '18px',
                  }}
                >
                  💰
                </Box>
                <Box style={{ flex: 1 }}>
                  <Text
                    style={{
                      fontWeight: 600,
                      fontSize: '16px',
                      color: '#2B2B2B',
                      marginBottom: '4px',
                    }}
                  >
                    Member Discounts
                  </Text>
                  <Text style={{ fontSize: '14px', color: '#8B8680' }}>
                    Reduced pricing on workshops and special events
                  </Text>
                </Box>
                <Badge color="green" size="sm">Active</Badge>
              </Box>
            </Stack>
          </Paper>

          {/* Community Standing */}
          <Paper
            style={{
              background: '#FFF8F0',
              padding: '24px',
              transition: 'all 0.3s ease',
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
              Community Standing
            </Title>

            <Stack gap="md">
              <Box>
                <Group justify="space-between" mb="xs">
                  <Text style={{ fontSize: '14px', fontWeight: 600, color: '#2B2B2B' }}>
                    Trust Level
                  </Text>
                  <Text style={{ fontSize: '14px', color: '#8B8680' }}>
                    Good Standing
                  </Text>
                </Group>
                <Progress 
                  value={85} 
                  color="#228B22" 
                  size="sm" 
                  style={{ background: 'rgba(183, 109, 117, 0.1)' }}
                />
              </Box>

              <Box>
                <Group justify="space-between" mb="xs">
                  <Text style={{ fontSize: '14px', fontWeight: 600, color: '#2B2B2B' }}>
                    Event Attendance
                  </Text>
                  <Text style={{ fontSize: '14px', color: '#8B8680' }}>
                    Regular Attendee
                  </Text>
                </Group>
                <Progress 
                  value={72} 
                  color="#DAA520" 
                  size="sm"
                  style={{ background: 'rgba(183, 109, 117, 0.1)' }}
                />
              </Box>

              <Box>
                <Group justify="space-between" mb="xs">
                  <Text style={{ fontSize: '14px', fontWeight: 600, color: '#2B2B2B' }}>
                    Community Participation
                  </Text>
                  <Text style={{ fontSize: '14px', color: '#8B8680' }}>
                    Active Member
                  </Text>
                </Group>
                <Progress 
                  value={60} 
                  color="#880124" 
                  size="sm"
                  style={{ background: 'rgba(183, 109, 117, 0.1)' }}
                />
              </Box>
            </Stack>

            <Box
              style={{
                background: 'rgba(34, 139, 34, 0.05)',
                border: '1px solid rgba(34, 139, 34, 0.2)',
                borderRadius: '6px',
                padding: '16px',
                marginTop: '20px',
              }}
            >
              <Text
                style={{
                  fontSize: '14px',
                  color: '#228B22',
                  fontWeight: 600,
                  marginBottom: '4px',
                }}
              >
                ✓ Member in Good Standing
              </Text>
              <Text style={{ fontSize: '13px', color: '#8B8680' }}>
                You maintain good standing in the community with regular participation and positive interactions.
              </Text>
            </Box>
          </Paper>

          {/* Account Actions */}
          <Paper
            style={{
              background: '#FFF8F0',
              padding: '24px',
              transition: 'all 0.3s ease',
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
              Membership Actions
            </Title>

            <Group>
              <button className="btn btn-secondary">
                View Community Guidelines
              </button>
              
              <button className="btn btn-secondary">
                Contact Support
              </button>

              {/* TODO: Add membership upgrade options when available */}
            </Group>

            <Box
              style={{
                background: 'rgba(183, 109, 117, 0.05)',
                border: '1px solid rgba(183, 109, 117, 0.2)',
                borderRadius: '6px',
                padding: '16px',
                marginTop: '20px',
              }}
            >
              <Text
                style={{
                  fontSize: '14px',
                  color: '#4A4A4A',
                  lineHeight: 1.6,
                }}
              >
                <strong>Need Help?</strong> If you have questions about your membership status or benefits, 
                please contact our community moderators through the support channels.
              </Text>
            </Box>
          </Paper>
        </Stack>
      </Box>
    </DashboardLayout>
  );
};