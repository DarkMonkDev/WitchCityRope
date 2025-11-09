import React from 'react';
import { Container, Title, Text, Tabs } from '@mantine/core';
import { useSearchParams } from 'react-router-dom';
import { EmailCategoryPanel } from '../../components/email-templates/EmailCategoryPanel';

/**
 * Email Templates Admin Management Page
 *
 * Features:
 * - Tabbed interface for 5 categories (Vetting, Events, Admin, Incident, Ad Hoc)
 * - URL query parameter support for tab state (?tab=vetting)
 * - Reuses EmailCategoryPanel component for each category
 *
 * Based on UI Designer Handoff specifications
 */
export const EmailTemplatesAdminPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get('tab') || 'vetting';

  // Handle tab change with URL sync
  const handleTabChange = (value: string | null) => {
    if (value) {
      setSearchParams({ tab: value });
    }
  };

  return (
    <Container size="xl" py="xl">
      {/* Page Title */}
      <Title
        order={1}
        mb="md"
        style={{
          fontFamily: "'Montserrat', sans-serif",
          fontSize: '32px',
          fontWeight: 800,
          color: '#880124',
          textTransform: 'uppercase',
          letterSpacing: '-0.5px',
        }}
      >
        Email Templates Management
      </Title>

      <Text size="sm" c="dimmed" mb="xl">
        Manage global email templates used across all events and system notifications. Changes here
        affect all future emails unless overridden at the event level.
      </Text>

      {/* Tabbed Interface */}
      <Tabs
        value={activeTab}
        onChange={handleTabChange}
        styles={{
          tab: {
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '15px',
            fontWeight: 600,
            textTransform: 'uppercase',
            letterSpacing: '1px',
            color: '#4A4A4A',
            padding: 'var(--mantine-spacing-md) var(--mantine-spacing-lg)',
            borderBottom: '3px solid transparent',
            transition: 'all 0.3s ease',
            '&[data-active]': {
              color: '#880124',
              borderBottomColor: '#880124',
              fontWeight: 700,
            },
            '&:hover': {
              backgroundColor: 'rgba(136, 1, 36, 0.05)',
            },
          },
        }}
      >
        <Tabs.List>
          <Tabs.Tab value="vetting">Vetting</Tabs.Tab>
          <Tabs.Tab value="events">Events</Tabs.Tab>
          <Tabs.Tab value="admin">Admin</Tabs.Tab>
          <Tabs.Tab value="incident">Incident</Tabs.Tab>
          <Tabs.Tab value="adhoc">Ad Hoc</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="vetting" pt="xl">
          <EmailCategoryPanel category="Vetting" />
        </Tabs.Panel>

        <Tabs.Panel value="events" pt="xl">
          <EmailCategoryPanel category="Events" />
        </Tabs.Panel>

        <Tabs.Panel value="admin" pt="xl">
          <EmailCategoryPanel category="Admin" />
        </Tabs.Panel>

        <Tabs.Panel value="incident" pt="xl">
          <EmailCategoryPanel category="Incident" />
        </Tabs.Panel>

        <Tabs.Panel value="adhoc" pt="xl">
          <EmailCategoryPanel category="AdHoc" />
        </Tabs.Panel>
      </Tabs>
    </Container>
  );
};
