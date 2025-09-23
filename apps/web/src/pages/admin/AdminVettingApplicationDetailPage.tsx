import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Container, Button, Group, Title, Text } from '@mantine/core';
import { IconArrowLeft } from '@tabler/icons-react';
import { VettingApplicationDetail } from '../../features/admin/vetting/components/VettingApplicationDetail';

/**
 * Admin Vetting Application Detail Page
 *
 * This is the dedicated page for viewing a single vetting application.
 * Following the wireframe specification, this is a SEPARATE PAGE (not a modal).
 *
 * Route: /admin/vetting/applications/:applicationId
 */
export const AdminVettingApplicationDetailPage: React.FC = () => {
  const { applicationId } = useParams<{ applicationId: string }>();
  const navigate = useNavigate();

  const handleBackToList = () => {
    navigate('/admin/vetting');
  };

  if (!applicationId) {
    return (
      <Container size="xl" py="xl">
        <Text c="red">Invalid application ID</Text>
        <Button leftSection={<IconArrowLeft size={16} />} onClick={handleBackToList}>
          Back to Applications
        </Button>
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