import React from 'react';
import { Container, Title, Text, SimpleGrid, Paper, Box, Skeleton, Alert, Button } from '@mantine/core';
import {
  IconUsers,
  IconUserCheck,
  IconUserX,
  IconShieldCheck,
  IconClipboardCheck,
  IconAlertCircle,
  IconRefresh,
} from '@tabler/icons-react';
import { useUserSummary, type UserSummaryData } from '../../features/admin/reports/hooks/useUserSummary';

/**
 * Reports Dashboard home page — shows user summary statistics
 * in a responsive card grid. Matches AdminDashboardPage styling conventions
 * (ivory Paper cards, burgundy accents, Montserrat headings).
 */

interface StatCardConfig {
  key: keyof UserSummaryData;
  label: string;
  icon: React.FC<{ size?: number | string; color?: string }>;
  color: string;
}

const statCards: StatCardConfig[] = [
  { key: 'totalUsers', label: 'Total Users', icon: IconUsers, color: '#880124' },
  { key: 'verifiedUsers', label: 'Verified', icon: IconUserCheck, color: '#228B22' },
  { key: 'unverifiedUsers', label: 'Unverified', icon: IconUserX, color: '#DAA520' },
  { key: 'vettedMembers', label: 'Vetted Members', icon: IconShieldCheck, color: '#614B79' },
  { key: 'vettingApplicationsSubmitted', label: 'Applications Submitted', icon: IconClipboardCheck, color: '#9b4a75' },
];

export const AdminReportsHomePage: React.FC = () => {
  const { data, isLoading, error, refetch } = useUserSummary();

  return (
    <Container size="xl" py="xl">
      {/* Page Title — matches AdminDashboardPage title styling */}
      <Box mb="xl">
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
          Reports Dashboard
        </Title>
      </Box>

      {/* Section Heading */}
      <Text
        size="lg"
        fw={600}
        mb="lg"
        style={{
          fontFamily: "'Montserrat', sans-serif",
          color: '#2B2B2B',
        }}
      >
        Community Overview
      </Text>

      {/* Error State */}
      {error && (
        <Alert
          icon={<IconAlertCircle />}
          color="red"
          title="Error Loading Summary"
          mb="lg"
        >
          <Text>{error.message || 'Failed to load user summary data.'}</Text>
          <Button
            variant="light"
            color="red"
            size="xs"
            mt="sm"
            onClick={() => refetch()}
            leftSection={<IconRefresh size={14} />}
          >
            Retry
          </Button>
        </Alert>
      )}

      {/* Stat Cards Grid — responsive from 1 column (mobile) to 5 columns (desktop) */}
      <SimpleGrid cols={{ base: 1, xs: 2, sm: 3, md: 5 }} spacing="lg">
        {statCards.map((card) => {
          const Icon = card.icon;
          const value = data?.[card.key];

          return (
            <Paper
              key={card.key}
              shadow="sm"
              radius="md"
              p="lg"
              style={{
                background: '#FFF8F0',
                border: '1px solid rgba(136, 1, 36, 0.1)',
              }}
            >
              {isLoading ? (
                /* Loading skeleton matches card content layout */
                <>
                  <Skeleton height={32} width={32} radius="sm" mb="md" />
                  <Skeleton height={28} width={60} mb="xs" />
                  <Skeleton height={16} width={100} />
                </>
              ) : (
                <>
                  <Box mb="md">
                    <Icon size={32} color={card.color} />
                  </Box>
                  <Text size="xl" fw={700} style={{ color: card.color }}>
                    {value ?? 0}
                  </Text>
                  <Text size="sm" c="dimmed">
                    {card.label}
                  </Text>
                </>
              )}
            </Paper>
          );
        })}
      </SimpleGrid>
    </Container>
  );
};
