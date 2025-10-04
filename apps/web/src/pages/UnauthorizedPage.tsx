import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Title, Text, Button, Paper, Stack, Group } from '@mantine/core';
import { IconLock, IconHome, IconArrowLeft } from '@tabler/icons-react';

/**
 * Unauthorized Access Page (HTTP 403)
 *
 * Displayed when an authenticated user attempts to access a resource
 * they don't have permission to view (e.g., non-admin accessing admin routes)
 */
export const UnauthorizedPage: React.FC = () => {
  const navigate = useNavigate();

  const handleGoHome = () => {
    navigate('/');
  };

  const handleGoBack = () => {
    navigate(-1);
  };

  return (
    <Container size="sm" py="xl">
      <Paper
        shadow="md"
        p="xl"
        radius="md"
        withBorder
        style={{
          textAlign: 'center',
          backgroundColor: '#FFF5F5'
        }}
      >
        <Stack gap="lg" align="center">
          <IconLock size={64} color="#C92A2A" />

          <Title
            order={1}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '32px',
              fontWeight: 800,
              color: '#C92A2A',
              textTransform: 'uppercase'
            }}
          >
            Access Denied
          </Title>

          <Text size="lg" c="dimmed">
            You do not have permission to access this page.
          </Text>

          <Text size="sm" c="dimmed">
            This area is restricted to administrators only. If you believe you should have access,
            please contact the site administrator.
          </Text>

          <Group gap="md" mt="md">
            <Button
              leftSection={<IconArrowLeft size={16} />}
              variant="outline"
              color="gray"
              onClick={handleGoBack}
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
              Go Back
            </Button>

            <Button
              leftSection={<IconHome size={16} />}
              variant="filled"
              color="blue"
              onClick={handleGoHome}
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
              Go to Home
            </Button>
          </Group>
        </Stack>
      </Paper>
    </Container>
  );
};
