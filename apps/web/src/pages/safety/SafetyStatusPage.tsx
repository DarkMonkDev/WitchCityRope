// Safety Status Page
// Public page for tracking incident status by reference number

import React, { useState, useEffect } from 'react';
import {
  Container,
  Box,
  Paper,
  Title,
  Text,
  TextInput,
  Button,
  Group,
  Stack,
  Alert,
  Badge,
  Loader
} from '@mantine/core';
import { IconSearch, IconInfoCircle, IconCheck, IconClock } from '@tabler/icons-react';
import { useSearchParams } from 'react-router-dom';
import { useIncidentStatus } from '../../features/safety/hooks/useSafetyIncidents';

export function SafetyStatusPage() {
  const [searchParams] = useSearchParams();
  const [referenceNumber, setReferenceNumber] = useState(searchParams.get('ref') || '');
  const [shouldFetch, setShouldFetch] = useState(false);
  
  // Fetch status when reference number is provided
  const {
    data: statusData,
    isLoading,
    error,
    refetch
  } = useIncidentStatus(referenceNumber, shouldFetch && !!referenceNumber);
  
  // Auto-fetch if reference number comes from URL
  useEffect(() => {
    const urlRef = searchParams.get('ref');
    if (urlRef) {
      setReferenceNumber(urlRef);
      setShouldFetch(true);
    }
  }, [searchParams]);
  
  const handleSearch = () => {
    if (referenceNumber.trim()) {
      setShouldFetch(true);
    }
  };
  
  const handleKeyPress = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter') {
      handleSearch();
    }
  };
  
  const formatLastUpdated = (isoString: string) => {
    const date = new Date(isoString);
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  };
  
  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'new': return 'blue';
      case 'inprogress': return 'yellow';
      case 'resolved': return 'green';
      case 'archived': return 'gray';
      default: return 'gray';
    }
  };
  
  const getStatusLabel = (status: string) => {
    switch (status.toLowerCase()) {
      case 'new': return 'New';
      case 'inprogress': return 'In Progress';
      case 'resolved': return 'Resolved';
      case 'archived': return 'Archived';
      default: return status;
    }
  };
  
  return (
    <Container size="md" py="xl">
      <Stack gap="lg">
        {/* Header */}
        <Box ta="center">
          <Title order={1} size="h2" mb="xs">
            Track Your Safety Report
          </Title>
          <Text c="dimmed">
            Enter your reference number to check the status of your incident report
          </Text>
        </Box>
        
        {/* Search Form */}
        <Paper shadow="sm" p="xl" radius="md">
          <Stack gap="md">
            <Title order={3} size="h4">Reference Number Lookup</Title>
            
            <Group align="end">
              <TextInput
                label="Reference Number"
                placeholder="SAF-YYYYMMDD-0000"
                value={referenceNumber}
                onChange={(event) => setReferenceNumber(event.currentTarget.value)}
                onKeyDown={handleKeyPress}
                style={{ flex: 1 }}
                leftSection={<IconSearch size={16} />}
              />
              <Button 
                onClick={handleSearch}
                loading={isLoading}
                disabled={!referenceNumber.trim()}
              >
                Check Status
              </Button>
            </Group>
            
            <Alert variant="light" color="blue" icon={<IconInfoCircle size={16} />}>
              <Text size="sm">
                Your reference number was provided when you submitted your report. 
                It follows the format SAF-YYYYMMDD-0000 (e.g., SAF-20250912-0001).
              </Text>
            </Alert>
          </Stack>
        </Paper>
        
        {/* Loading State */}
        {isLoading && (
          <Paper shadow="sm" p="xl" radius="md">
            <Group justify="center">
              <Loader size="lg" />
              <Text>Checking incident status...</Text>
            </Group>
          </Paper>
        )}
        
        {/* Error State */}
        {error && shouldFetch && (
          <Alert variant="light" color="red">
            <Stack gap="xs">
              <Text size="sm" fw={500}>Unable to find incident</Text>
              <Text size="sm">
                Please check your reference number and try again. If you continue to have issues, 
                contact the safety team at safety@witchcityrope.com.
              </Text>
            </Stack>
          </Alert>
        )}
        
        {/* Status Results */}
        {statusData && !error && (
          <Paper shadow="sm" p="xl" radius="md">
            <Stack gap="lg">
              <Group justify="space-between" align="center">
                <Box>
                  <Title order={3} size="h4" mb="xs">
                    Incident Status
                  </Title>
                  <Text c="dimmed" size="sm">
                    Reference: {statusData.referenceNumber}
                  </Text>
                </Box>
                <Badge 
                  size="lg" 
                  color={getStatusColor(statusData.status)}
                  leftSection={
                    statusData.status.toLowerCase() === 'resolved' 
                      ? <IconCheck size={12} />
                      : <IconClock size={12} />
                  }
                >
                  {getStatusLabel(statusData.status)}
                </Badge>
              </Group>
              
              <Alert 
                variant="light" 
                color={getStatusColor(statusData.status)}
                icon={
                  statusData.status.toLowerCase() === 'resolved' 
                    ? <IconCheck size={16} />
                    : <IconClock size={16} />
                }
              >
                <Stack gap="xs">
                  <Text size="sm" fw={500}>
                    {statusData.status.toLowerCase() === 'new' && 'Your report has been received'}
                    {statusData.status.toLowerCase() === 'inprogress' && 'Your report is being investigated'}
                    {statusData.status.toLowerCase() === 'resolved' && 'Your report has been resolved'}
                    {statusData.status.toLowerCase() === 'archived' && 'Your report has been archived'}
                  </Text>
                  <Text size="sm">
                    {statusData.status.toLowerCase() === 'new' && 
                      'The safety team has been notified and will review your report shortly.'}
                    {statusData.status.toLowerCase() === 'inprogress' && 
                      'The safety team is actively investigating this incident.'}
                    {statusData.status.toLowerCase() === 'resolved' && 
                      'The safety team has completed their investigation and taken appropriate action.'}
                    {statusData.status.toLowerCase() === 'archived' && 
                      'This incident has been resolved and archived for record keeping.'}
                  </Text>
                  <Text size="xs" c="dimmed">
                    Last updated: {formatLastUpdated(statusData.lastUpdated)}
                  </Text>
                </Stack>
              </Alert>
              
              {statusData.canProvideMoreInfo && (
                <Alert variant="light" color="blue" icon={<IconInfoCircle size={16} />}>
                  <Stack gap="xs">
                    <Text size="sm" fw={500}>Additional Information Welcome</Text>
                    <Text size="sm">
                      If you have additional information about this incident, you can contact 
                      the safety team at safety@witchcityrope.com and reference your incident number.
                    </Text>
                  </Stack>
                </Alert>
              )}
              
              <Group justify="center" mt="md">
                <Button variant="light" onClick={() => refetch()}>
                  Refresh Status
                </Button>
              </Group>
            </Stack>
          </Paper>
        )}
        
        {/* Help Information */}
        <Paper shadow="sm" p="md" radius="md" style={{ backgroundColor: '#F8F9FA' }}>
          <Stack gap="sm">
            <Title order={4} size="h5">Need Help?</Title>
            <Group justify="space-around">
              <Box ta="center">
                <Text size="sm" fw={500}>Safety Team</Text>
                <Text size="sm" c="blue">safety@witchcityrope.com</Text>
              </Box>
              <Box ta="center">
                <Text size="sm" fw={500}>Crisis Support</Text>
                <Text size="sm" c="blue">Available 24/7</Text>
              </Box>
            </Group>
          </Stack>
        </Paper>
      </Stack>
    </Container>
  );
}