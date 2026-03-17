import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Title, Group, Button, Alert } from '@mantine/core';
import { IconMail, IconClock, IconLock } from '@tabler/icons-react';
import { VettingApplicationsList } from '../../features/admin/vetting/components/VettingApplicationsList';
import { OnHoldModal } from '../../features/admin/vetting/components/OnHoldModal';
// SendReminderModal is now per-application (on the detail page), not bulk
import { useUser } from '../../stores/authStore';
import { hasRole } from '../../utils/roleUtils';

/**
 * Admin Vetting Applications List Page
 *
 * SECURITY: This page requires Administrator role
 * - Route-level protection via adminLoader
 * - Component-level verification (defense-in-depth)
 *
 * This page shows the list of vetting applications following the wireframe.
 * Row clicks navigate to the detail page at /admin/vetting/applications/:id
 *
 * Route: /admin/vetting
 */
export const AdminVettingPage: React.FC = () => {
  const navigate = useNavigate();
  const user = useUser();

  // Component-level role verification (defense-in-depth)
  useEffect(() => {
    if (user && !hasRole(user, 'Administrator')) {
      console.error('AdminVettingPage: Unauthorized access attempt by non-admin user:', user.email);
      navigate('/unauthorized', { replace: true });
    }
  }, [user, navigate]);

  // Show error if somehow accessed without proper role
  if (!user || !hasRole(user, 'Administrator')) {
    return (
      <Container size="xl" py="xl">
        <Alert icon={<IconLock size={16} />} color="red" title="Access Denied">
          You do not have permission to access this page.
        </Alert>
      </Container>
    );
  }

  // State for selected applications and modals
  const [selectedApplications, setSelectedApplications] = useState<Set<string>>(new Set());
  const [selectedApplicationsData, setSelectedApplicationsData] = useState<any[]>([]);
  const [onHoldModalOpen, setOnHoldModalOpen] = useState(false);
  const handleEmailTemplatesClick = () => {
    navigate('/admin/email-templates?tab=vetting');
  };

  const handlePutOnHoldClick = () => {
    if (selectedApplications.size === 0) return;
    setOnHoldModalOpen(true);
  };

  const handleSelectionChange = useCallback((selectedIds: Set<string>, applicationsData: any[]) => {
    setSelectedApplications(selectedIds);
    setSelectedApplicationsData(applicationsData);
  }, []);

  const hasSelectedApplications = selectedApplications.size > 0;

  return (
    <Container size="xl" py="xl">
      {/* Header */}
      <Group justify="space-between" align="center" mb="xl">
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

        {/* Action buttons aligned with title */}
        <Group gap="md">
          <Button
            leftSection={<IconClock size={16} />}
            variant="outline"
            color="orange"
            size="md"
            onClick={handlePutOnHoldClick}
            disabled={!hasSelectedApplications}
            styles={{
              root: {
                borderColor: '#FF8C00',
                color: '#FF8C00',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            PUT ON HOLD ({selectedApplications.size})
          </Button>

          <Button
            leftSection={<IconMail size={16} />}
            variant="filled"
            color="blue"
            size="md"
            onClick={handleEmailTemplatesClick}
            styles={{
              root: {
                backgroundColor: '#4A90E2',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            EMAIL TEMPLATES
          </Button>
        </Group>
      </Group>

      {/* Applications List */}
      <VettingApplicationsList onSelectionChange={handleSelectionChange} />

      {/* Bulk Action Modals */}
      {/* Put on Hold requires selections */}
      {hasSelectedApplications && selectedApplicationsData.length > 0 && (
        <OnHoldModal
          opened={onHoldModalOpen}
          onClose={() => setOnHoldModalOpen(false)}
          applicationIds={Array.from(selectedApplications)}
          applicantNames={selectedApplicationsData.map(app =>
            [(app as any).firstName, (app as any).lastName].filter(Boolean).join(' ') || app.sceneName || 'Unknown'
          )}
          onSuccess={() => {
            setSelectedApplications(new Set());
            setSelectedApplicationsData([]);
            // TODO: Refresh the applications list
          }}
        />
      )}

    </Container>
  );
};