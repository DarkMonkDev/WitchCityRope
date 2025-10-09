import React, { useEffect } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { Container, Button, Group, Title, Text, Alert, Stack } from '@mantine/core';
import { IconArrowLeft, IconAlertCircle, IconLock } from '@tabler/icons-react';
import { VettingApplicationDetail } from '../../features/admin/vetting/components/VettingApplicationDetail';
import { useUser } from '../../stores/authStore';

/**
 * Admin Vetting Application Detail Page
 *
 * SECURITY: This page requires Administrator role
 * - Route-level protection via adminLoader
 * - Component-level verification (defense-in-depth)
 *
 * This is the dedicated page for viewing a single vetting application.
 * Following the wireframe specification, this is a SEPARATE PAGE (not a modal).
 *
 * Route: /admin/vetting/applications/:applicationId
 */
export const AdminVettingApplicationDetailPage: React.FC = () => {
  const { applicationId } = useParams<{ applicationId: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const user = useUser();

  // Component-level role verification (defense-in-depth)
  useEffect(() => {
    if (user && user.role !== 'Administrator') {
      console.error('AdminVettingApplicationDetailPage: Unauthorized access attempt by non-admin user:', user.email);
      navigate('/unauthorized', { replace: true });
    }
  }, [user, navigate]);

  const handleBackToList = () => {
    navigate('/admin/vetting');
  };

  // Show error if somehow accessed without proper role
  if (!user || user.role !== 'Administrator') {
    return (
      <Container size="xl" py="xl">
        <Alert icon={<IconLock size={16} />} color="red" title="Access Denied">
          You do not have permission to access this page.
        </Alert>
      </Container>
    );
  }

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
    <Container size="xl" pt="sm" pb="xl">
      {/* Application Detail Component */}
      <VettingApplicationDetail
        applicationId={applicationId}
        onBack={handleBackToList}
      />
    </Container>
  );
};