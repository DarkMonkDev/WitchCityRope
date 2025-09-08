import React from 'react';
import { Card, Text, Group, Badge, Button, Box } from '@mantine/core';

interface DashboardCardProps {
  title: string;
  description?: string;
  status?: string;
  statusColor?: string;
  action?: string;
  onActionClick?: () => void;
  icon?: React.ReactNode;
  loading?: boolean;
  children?: React.ReactNode;
}

/**
 * Reusable Dashboard Card Component
 * Used across all dashboard sections for consistent UI
 * Follows WitchCityRope Design System v7 styling
 */
export const DashboardCard: React.FC<DashboardCardProps> = ({
  title,
  description,
  status,
  statusColor = '#880124',
  action,
  onActionClick,
  icon,
  loading = false,
  children
}) => {
  return (
    <Card
      shadow="sm"
      padding="lg"
      radius="md"
      withBorder
      style={{
        background: 'linear-gradient(135deg, #f8f4e6, #e8ddd4)',
        borderColor: 'rgba(183, 109, 117, 0.2)',
        transition: 'all 0.3s ease',
        height: '100%',
      }}
      onMouseEnter={(e) => {
        if (!loading) {
          e.currentTarget.style.transform = 'translateY(-2px)';
          e.currentTarget.style.boxShadow = '0 4px 12px rgba(136, 1, 36, 0.15)';
        }
      }}
      onMouseLeave={(e) => {
        if (!loading) {
          e.currentTarget.style.transform = 'translateY(0)';
          e.currentTarget.style.boxShadow = 'var(--mantine-shadow-sm)';
        }
      }}
    >
      {/* Header Section */}
      <Card.Section withBorder inheritPadding py="sm">
        <Group justify="space-between" align="center">
          <Group gap="xs" align="center">
            {icon && (
              <Box style={{ fontSize: '20px', color: '#880124' }}>
                {icon}
              </Box>
            )}
            <Text
              size="lg"
              fw={700}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                color: '#2B2B2B',
                letterSpacing: '0.5px',
              }}
            >
              {title}
            </Text>
          </Group>
          
          {status && (
            <Badge
              color="wcr"
              variant="light"
              style={{
                backgroundColor: `${statusColor}20`,
                color: statusColor,
                border: `1px solid ${statusColor}40`,
                fontWeight: 600,
                fontSize: '11px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              {status}
            </Badge>
          )}
        </Group>
      </Card.Section>

      {/* Content Section */}
      <Box mt="sm" mb="md" style={{ flexGrow: 1 }}>
        {description && (
          <Text
            size="sm"
            c="dimmed"
            style={{
              color: '#8B8680',
              lineHeight: 1.5,
              marginBottom: children ? '12px' : 0,
            }}
          >
            {description}
          </Text>
        )}
        
        {children}
      </Box>

      {/* Action Section */}
      {action && (
        <Button
          variant="light"
          color="wcr"
          fullWidth
          onClick={onActionClick}
          loading={loading}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontWeight: 600,
            fontSize: '14px',
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
            background: 'rgba(136, 1, 36, 0.1)',
            color: '#880124',
            border: '1px solid rgba(136, 1, 36, 0.3)',
            borderRadius: '6px',
            transition: 'all 0.3s ease',
          }}
          onMouseEnter={(e) => {
            if (!loading) {
              e.currentTarget.style.background = '#880124';
              e.currentTarget.style.color = '#FFF8F0';
            }
          }}
          onMouseLeave={(e) => {
            if (!loading) {
              e.currentTarget.style.background = 'rgba(136, 1, 36, 0.1)';
              e.currentTarget.style.color = '#880124';
            }
          }}
        >
          {action}
        </Button>
      )}
    </Card>
  );
};