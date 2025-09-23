import React, { useEffect } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { Container, Button, Group, Title, Text, Alert, Stack } from '@mantine/core';
import { IconArrowLeft, IconAlertCircle } from '@tabler/icons-react';
import { VettingApplicationDetail } from '../../features/admin/vetting/components/VettingApplicationDetail';

/**
 * Admin Vetting Application Detail Page
 *
 * This is the dedicated page for viewing a single vetting application.
 * Following the wireframe specification, this is a SEPARATE PAGE (not a modal).
 *
 * Route: /admin/vetting/applications/:applicationId
 *
 * Enhanced with better error handling and debugging to identify routing issues.
 */
export const AdminVettingApplicationDetailPage: React.FC = () => {
  const { applicationId } = useParams<{ applicationId: string }>();
  const navigate = useNavigate();
  const location = useLocation();

  const handleBackToList = () => {
    navigate('/admin/vetting');
  };

  // Enhanced validation and debugging
  console.log('AdminVettingApplicationDetailPage rendered:', {
    applicationId,
    pathname: location.pathname,
    params: useParams(),
    timestamp: new Date().toISOString()
  });

  // Add effect to track route changes
  useEffect(() => {
    console.log('AdminVettingApplicationDetailPage mounted/updated:', {
      applicationId,
      pathname: location.pathname,
      timestamp: new Date().toISOString()
    });
  }, [applicationId, location.pathname]);

  if (!applicationId) {
    return (
      <Container size="xl" py="xl">
        <Stack gap="md">
          <Alert icon={<IconAlertCircle size={16} />} color="red" title="Invalid Application ID">
            <Text>No application ID was provided in the URL.</Text>
            <Text size="sm" c="dimmed" mt="xs">
              Current path: {location.pathname}
            </Text>
          </Alert>
          <Button
            leftSection={<IconArrowLeft size={16} />}
            onClick={handleBackToList}
            styles={{
              root: {
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Back to Applications
          </Button>
        </Stack>
      </Container>
    );
  }

  // Validate applicationId format (should be a valid GUID or number)
  const isValidId = /^[a-f\d]{8}(-[a-f\d]{4}){4}[a-f\d]{8}$/i.test(applicationId) || /^\d+$/.test(applicationId);

  if (!isValidId) {
    return (
      <Container size="xl" py="xl">
        <Stack gap="md">
          <Alert icon={<IconAlertCircle size={16} />} color="orange" title="Invalid Application ID Format">
            <Text>The provided application ID "{applicationId}" is not in a valid format.</Text>
            <Text size="sm" c="dimmed" mt="xs">
              Expected: GUID (e.g., 123e4567-e89b-12d3-a456-426614174000) or numeric ID (e.g., 2)
            </Text>
          </Alert>
          <Button
            leftSection={<IconArrowLeft size={16} />}
            onClick={handleBackToList}
            styles={{
              root: {
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Back to Applications
          </Button>
        </Stack>
      </Container>
    );
  }

  return (
    <Container size="xl" py="xl">
      {/* Application Detail Component */}
      <VettingApplicationDetail
        applicationId={applicationId}
        onBack={handleBackToList}
      />
    </Container>
  );
};