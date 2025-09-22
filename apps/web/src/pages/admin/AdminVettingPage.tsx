import React, { useState } from 'react';
import { Container, Title, Text, Group } from '@mantine/core';
import { VettingApplicationsList } from '../../features/admin/vetting/components/VettingApplicationsList';
import { VettingApplicationDetail } from '../../features/admin/vetting/components/VettingApplicationDetail';

export const AdminVettingPage: React.FC = () => {
  const [selectedApplicationId, setSelectedApplicationId] = useState<string | null>(null);

  const handleViewApplication = (applicationId: string) => {
    setSelectedApplicationId(applicationId);
  };

  const handleBackToList = () => {
    setSelectedApplicationId(null);
  };

  return (
    <Container size="xl" py="xl">
      {/* Header */}
      {!selectedApplicationId && (
        <Group justify="space-between" align="center" mb="xl">
          <div>
            <Title
              order={1}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '32px',
                fontWeight: 800,
                color: '#880124',
                textTransform: 'uppercase',
                letterSpacing: '-0.5px',
              }}
            >
              Vetting Applications
            </Title>
            <Text size="lg" c="dimmed" mt="xs">
              Review and manage member vetting applications
            </Text>
          </div>
        </Group>
      )}

      {/* Content */}
      {selectedApplicationId ? (
        <VettingApplicationDetail
          applicationId={selectedApplicationId}
          onBack={handleBackToList}
        />
      ) : (
        <VettingApplicationsList
          onViewApplication={handleViewApplication}
        />
      )}
    </Container>
  );
};