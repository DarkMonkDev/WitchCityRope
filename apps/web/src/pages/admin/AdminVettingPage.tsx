import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Title, Text, Group, Button, Alert } from '@mantine/core';
import { IconMail, IconClock, IconAlertTriangle, IconLock } from '@tabler/icons-react';
import { VettingApplicationsList } from '../../features/admin/vetting/components/VettingApplicationsList';
import { OnHoldModal } from '../../features/admin/vetting/components/OnHoldModal';
import { SendReminderModal } from '../../features/admin/vetting/components/SendReminderModal';
import { useUser } from '../../stores/authStore';

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
    if (user && user.role !== 'Administrator') {
      console.error('AdminVettingPage: Unauthorized access attempt by non-admin user:', user.email);
      navigate('/unauthorized', { replace: true });
    }
  }, [user, navigate]);

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

  // State for selected applications and modals
  const [selectedApplications, setSelectedApplications] = useState<Set<string>>(new Set());
  const [selectedApplicationsData, setSelectedApplicationsData] = useState<any[]>([]);
  const [onHoldModalOpen, setOnHoldModalOpen] = useState(false);
  const [sendReminderModalOpen, setSendReminderModalOpen] = useState(false);

  const handleEmailTemplatesClick = () => {
    navigate('/admin/vetting/email-templates');
  };

  const handlePutOnHoldClick = () => {
    if (selectedApplications.size === 0) return;
    setOnHoldModalOpen(true);
  };

  const handleSendReminderClick = () => {
    setSendReminderModalOpen(true);
  };

  const handleSelectionChange = (selectedIds: Set<string>, applicationsData: any[]) => {
    setSelectedApplications(selectedIds);
    setSelectedApplicationsData(applicationsData);
  };

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
            leftSection={<IconAlertTriangle size={16} />}
            variant="outline"
            color="blue"
            size="md"
            onClick={handleSendReminderClick}
            styles={{
              root: {
                borderColor: '#4A90E2',
                color: '#4A90E2',
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
            SEND REMINDER
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
            (app as any).realName || (app as any).fullName || app.sceneName || 'Unknown'
          )}
          onSuccess={() => {
            setSelectedApplications(new Set());
            setSelectedApplicationsData([]);
            // TODO: Refresh the applications list
          }}
        />
      )}

      {/* Send Reminder works independently of selections */}
      <SendReminderModal
        opened={sendReminderModalOpen}
        onClose={() => setSendReminderModalOpen(false)}
        onSuccess={() => {
          setSelectedApplications(new Set());
          setSelectedApplicationsData([]);
          // TODO: Refresh the applications list
        }}
      />
    </Container>
  );
};