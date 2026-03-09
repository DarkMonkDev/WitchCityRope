import React from 'react';
import { Container, Title, Text, Tabs, Divider } from '@mantine/core';
import { useSearchParams } from 'react-router-dom';
import { EmailCategoryPanel } from '../../components/email-templates/EmailCategoryPanel';
import { EmailTestDataTab } from '../../components/email-templates/EmailTestDataTab';
import classes from './EmailTemplatesAdminPage.module.css';

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
        style={{
          fontFamily: "'Montserrat', sans-serif",
          fontSize: '32px',
          fontWeight: 800,
          color: '#880124',
          textTransform: 'uppercase',
          letterSpacing: '-0.5px',
          marginBottom: '4px',
        }}
      >
        Email Templates Management
      </Title>

      <Text size="sm" c="dimmed" mb="lg">
        Manage global email templates used across all events and system notifications. Changes here
        affect all future emails unless overridden at the event level.
      </Text>

      {/* Tabbed Interface */}
      <Tabs
        value={activeTab}
        onChange={handleTabChange}
        variant="pills"
        radius="md"
        color="burgundy"
        classNames={{ tab: classes.tab }}
      >
        <Tabs.List>
          <Tabs.Tab value="vetting">Vetting</Tabs.Tab>
          <Tabs.Tab value="events">Events</Tabs.Tab>
          <Tabs.Tab value="admin">Admin</Tabs.Tab>
          <Tabs.Tab value="incident">Incident</Tabs.Tab>
          <Tabs.Tab value="adhoc">Ad Hoc</Tabs.Tab>
          <Tabs.Tab value="testdata">Test Data</Tabs.Tab>
        </Tabs.List>
        <Divider />

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

        <Tabs.Panel value="testdata" pt="xl">
          <EmailTestDataTab />
        </Tabs.Panel>
      </Tabs>
    </Container>
  );
};
