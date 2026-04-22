import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Title, Group, Button, Alert } from '@mantine/core';
import { IconMail, IconClock, IconLock } from '@tabler/icons-react';
import { VettingApplicationsList } from '../../features/admin/vetting/components/VettingApplicationsList';
import { OnHoldModal } from '../../features/admin/vetting/components/OnHoldModal';
import { SendReminderModal } from '../../features/admin/vetting/components/SendReminderModal';
import { useUser } from '../../stores/authStore';
import { hasAnyRole } from '../../utils/roleUtils';

/**
 * Admin Vetting Applications List Page
 *
 * SECURITY: This page requires Administrator or VettingTeam role
 * - Route-level protection via adminLoader (allows admin-capable roles)
 * - Component-level verification (defense-in-depth for Administrator + VettingTeam)
 *
 * This page shows the list of vetting applications following the wireframe.
 * Row clicks navigate to the detail page at /admin/vetting/applications/:id
 *
 * Route: /admin/vetting
 */
export const AdminVettingPage: React.FC = () => {
  // ALL hooks must be declared before any conditional return — this is the
  // Rules of Hooks. Previously the access-denied early-return sat between
  // the useEffect and the useState calls below, which meant the useState
  // hooks were called or skipped depending on whether the user was an admin.
  // That violates the rule that hooks must be called in the same order on
  // every render. The fix is to keep ALL hook calls grouped at the top and
  // do the conditional render afterwards.
  const navigate = useNavigate();
  const user = useUser();

  // State for selected applications and modals
  const [selectedApplications, setSelectedApplications] = useState<Set<string>>(new Set());
  const [selectedApplicationsData, setSelectedApplicationsData] = useState<any[]>([]);
  const [onHoldModalOpen, setOnHoldModalOpen] = useState(false);
  const [reminderModalOpen, setReminderModalOpen] = useState(false);

  // Component-level role verification (defense-in-depth — route guards
  // already enforce this, but we redirect any non-admin who somehow lands
  // here client-side).
  useEffect(() => {
    if (user && !hasAnyRole(user, ['Administrator', 'VettingTeam'])) {
      console.error('AdminVettingPage: Unauthorized access attempt by user without vetting access:', user.email);
      navigate('/unauthorized', { replace: true });
    }
  }, [user, navigate]);

  // Show error if somehow accessed without proper role
  if (!user || !hasAnyRole(user, ['Administrator', 'VettingTeam'])) {
    return (
      <Container size="xl" py="xl">
        <Alert icon={<IconLock size={16} />} color="red" title="Access Denied">
          You do not have permission to access this page.
        </Alert>
      </Container>
    );
  }

  const handleEmailTemplatesClick = () => {
    navigate('/admin/email-templates?tab=vetting');
  };

  const handlePutOnHoldClick = () => {
    if (selectedApplications.size === 0) return;
    setOnHoldModalOpen(true);
  };

  const handleSendReminderClick = () => {
    setReminderModalOpen(true);
  };

  const handleSelectionChange = useCallback((selectedIds: Set<string>, applicationsData: any[]) => {
    setSelectedApplications(selectedIds);
    setSelectedApplicationsData(applicationsData);
  }, []);

  const hasSelectedApplications = selectedApplications.size > 0;

  // Reminder eligibility: only applications in InterviewApproved status can
  // receive an interview reminder (the backend rejects any other status).
  // We filter the user's selection down to that subset so the bulk send
  // only targets eligible applications. The Send Reminder button only
  // appears when the selection contains at least one eligible app — but
  // mixed selections are allowed (ineligible rows are silently skipped,
  // which matches the user's stated spec for Q5).
  const reminderEligibleApplications = selectedApplicationsData.filter(
    app => app.status === 'InterviewApproved'
  );
  const hasReminderEligible = reminderEligibleApplications.length > 0;

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
          {/* Send Reminder bulk button — only renders when at least one
              selected application is in InterviewApproved status. The count
              shown in the label reflects only eligible apps, not the full
              selection, so reviewers see exactly how many emails will go
              out. The same SendReminderModal used on the detail page is
              reused here in bulk mode (see SendReminderModal docstring). */}
          {hasReminderEligible && (
            <Button
              leftSection={<IconMail size={16} />}
              variant="outline"
              color="orange"
              size="md"
              onClick={handleSendReminderClick}
              data-testid="bulk-send-reminder-button"
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
              SEND REMINDER ({reminderEligibleApplications.length})
            </Button>
          )}

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

      {/* Applications List — selection is controlled from this page so
          the parent can clear checkboxes after a successful bulk action.
          See VettingApplicationsList docstring for the rationale. */}
      <VettingApplicationsList
        selectedApplicationIds={selectedApplications}
        onSelectionChange={handleSelectionChange}
      />

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
            // List refresh is handled by OnHoldModal via React Query
            // invalidation — no manual refetch needed here.
          }}
        />
      )}

      {/* Bulk Send Reminder modal — opens with only the InterviewApproved
          subset of the selection. We deliberately pass the filtered subset
          (not the full selection) so the modal's recipient list and the
          fan-out calls only target eligible applications. */}
      {hasReminderEligible && (
        <SendReminderModal
          opened={reminderModalOpen}
          onClose={() => setReminderModalOpen(false)}
          applicationIds={reminderEligibleApplications.map(app => app.id)}
          applicantNames={reminderEligibleApplications.map(app => app.sceneName || 'Unknown')}
          onSuccess={() => {
            setSelectedApplications(new Set());
            setSelectedApplicationsData([]);
          }}
        />
      )}

    </Container>
  );
};