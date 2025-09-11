import React from 'react';
import { Box, Text, Stack, Group, Badge, Loader, Alert } from '@mantine/core';
import { DashboardCard } from './DashboardCard';
import { useNavigate } from 'react-router-dom';

// Mock data for demonstration - replace with real API integration later
interface EventRegistration {
  id: string;
  eventTitle: string;
  eventDate: string;
  registrationDate: string;
  status: 'upcoming' | 'completed' | 'cancelled';
  paymentStatus: 'paid' | 'pending' | 'refunded';
  attendanceStatus?: 'attended' | 'no-show' | 'pending';
}

// Mock registration data - replace with actual API call
const mockRegistrations: EventRegistration[] = [
  {
    id: '1',
    eventTitle: 'Beginner Rope Fundamentals',
    eventDate: '2025-09-15',
    registrationDate: '2025-08-20',
    status: 'upcoming',
    paymentStatus: 'paid',
    attendanceStatus: 'pending',
  },
  {
    id: '2',
    eventTitle: 'Advanced Suspension Workshop',
    eventDate: '2025-08-25',
    registrationDate: '2025-08-10',
    status: 'completed',
    paymentStatus: 'paid',
    attendanceStatus: 'attended',
  },
  {
    id: '3',
    eventTitle: 'Community Social Hour',
    eventDate: '2025-08-18',
    registrationDate: '2025-08-15',
    status: 'completed',
    paymentStatus: 'paid',
    attendanceStatus: 'attended',
  },
];

interface RegistrationHistoryProps {
  limit?: number;
  showOnlyUpcoming?: boolean;
}

/**
 * Registration History Widget for Dashboard
 * Shows user's event registration history with status indicators
 */
export const RegistrationHistory: React.FC<RegistrationHistoryProps> = ({ 
  limit = 5,
  showOnlyUpcoming = false 
}) => {
  const navigate = useNavigate();
  const [isLoading] = React.useState(false); // Will be replaced with actual loading state
  const [error] = React.useState(null); // Will be replaced with actual error state

  // Filter and limit registrations
  const displayRegistrations = React.useMemo(() => {
    let filteredRegs = mockRegistrations;
    
    if (showOnlyUpcoming) {
      filteredRegs = filteredRegs.filter(reg => reg.status === 'upcoming');
    }
    
    return filteredRegs
      .sort((a, b) => new Date(b.eventDate).getTime() - new Date(a.eventDate).getTime())
      .slice(0, limit);
  }, [limit, showOnlyUpcoming]);

  const handleViewAll = () => {
    navigate('/dashboard/registrations');
  };

  const getStatusBadge = (registration: EventRegistration) => {
    const { status, paymentStatus, attendanceStatus } = registration;
    
    if (status === 'upcoming') {
      return {
        text: paymentStatus === 'paid' ? 'Registered' : 'Payment Pending',
        color: paymentStatus === 'paid' ? '#228B22' : '#DAA520',
      };
    }
    
    if (status === 'completed') {
      return {
        text: attendanceStatus === 'attended' ? 'Attended' : 'No Show',
        color: attendanceStatus === 'attended' ? '#228B22' : '#DC143C',
      };
    }
    
    if (status === 'cancelled') {
      return {
        text: 'Cancelled',
        color: '#8B8680',
      };
    }
    
    return { text: 'Unknown', color: '#8B8680' };
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== new Date().getFullYear() ? 'numeric' : undefined,
    });
  };

  const totalAttended = mockRegistrations.filter(
    reg => reg.attendanceStatus === 'attended'
  ).length;

  return (
    <DashboardCard
      title="Registration History"
      icon="📋"
      status={`${totalAttended} Attended`}
      statusColor="#228B22"
      action="View Full History"
      onActionClick={handleViewAll}
      loading={isLoading}
    >
      {isLoading ? (
        <Box style={{ textAlign: 'center', padding: '20px 0' }}>
          <Loader size="md" color="#880124" />
          <Text size="sm" c="dimmed" mt="sm">
            Loading registrations...
          </Text>
        </Box>
      ) : error ? (
        <Alert 
          color="red" 
          variant="light"
          style={{ 
            background: 'rgba(220, 20, 60, 0.05)', 
            border: '1px solid rgba(220, 20, 60, 0.2)' 
          }}
        >
          Failed to load registration history
        </Alert>
      ) : displayRegistrations.length > 0 ? (
        <Stack gap="xs">
          {displayRegistrations.map((registration) => {
            const statusBadge = getStatusBadge(registration);
            
            return (
              <Box
                key={registration.id}
                style={{
                  padding: '12px',
                  background: '#FFF8F0',
                  borderRadius: '6px',
                  border: '1px solid rgba(183, 109, 117, 0.15)',
                  transition: 'all 0.2s ease',
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.borderColor = 'rgba(136, 1, 36, 0.3)';
                  e.currentTarget.style.transform = 'translateX(2px)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.borderColor = 'rgba(183, 109, 117, 0.15)';
                  e.currentTarget.style.transform = 'translateX(0)';
                }}
              >
                <Group justify="space-between" mb={4}>
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontSize: '14px',
                      fontWeight: 600,
                      color: '#2B2B2B',
                      lineHeight: 1.2,
                    }}
                  >
                    {registration.eventTitle}
                  </Text>
                  
                  <Badge
                    size="xs"
                    style={{
                      backgroundColor: `${statusBadge.color}20`,
                      color: statusBadge.color,
                      border: `1px solid ${statusBadge.color}40`,
                      fontWeight: 600,
                      fontSize: '10px',
                      textTransform: 'uppercase',
                    }}
                  >
                    {statusBadge.text}
                  </Badge>
                </Group>
                
                <Group justify="space-between" align="center">
                  <Text
                    size="xs"
                    style={{
                      color: '#8B8680',
                      fontFamily: "'Montserrat', sans-serif",
                    }}
                  >
                    Event: {formatDate(registration.eventDate)}
                  </Text>
                  
                  <Text
                    size="xs"
                    style={{
                      color: '#8B8680',
                      fontFamily: "'Montserrat', sans-serif",
                    }}
                  >
                    Registered: {formatDate(registration.registrationDate)}
                  </Text>
                </Group>
              </Box>
            );
          })}
          
          {/* Summary */}
          <Box
            style={{
              padding: '8px 12px',
              background: 'rgba(136, 1, 36, 0.05)',
              borderRadius: '4px',
              marginTop: '4px',
            }}
          >
            <Group justify="space-between">
              <Text
                size="xs"
                style={{
                  fontFamily: "'Montserrat', sans-serif",
                  fontWeight: 600,
                  color: '#880124',
                }}
              >
                Total Events: {mockRegistrations.length}
              </Text>
              <Text
                size="xs"
                style={{
                  fontFamily: "'Montserrat', sans-serif",
                  fontWeight: 600,
                  color: '#228B22',
                }}
              >
                Attended: {totalAttended}
              </Text>
            </Group>
          </Box>
        </Stack>
      ) : (
        <Box style={{ textAlign: 'center', padding: '20px 0' }}>
          <Text
            style={{
              fontSize: '24px',
              marginBottom: '8px',
              opacity: 0.5,
            }}
          >
            📋
          </Text>
          <Text
            size="sm"
            c="dimmed"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontWeight: 500,
            }}
          >
            No registrations yet
          </Text>
          <Text
            size="xs"
            c="dimmed"
            mt={4}
          >
            Start by browsing and registering for events
          </Text>
        </Box>
      )}
    </DashboardCard>
  );
};