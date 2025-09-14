// Test page for vetting system components
import React from 'react';
import { Container, Stack, Title, Button, Group, Divider } from '@mantine/core';
import { Link } from 'react-router-dom';

export const VettingTestPage: React.FC = () => {
  return (
    <Container size="md" py="xl">
      <Stack gap="xl">
        <Title order={1} c="wcr.7" ta="center">
          Vetting System Test
        </Title>
        
        <Divider />
        
        <Group justify="center">
          <Button component={Link} to="/vetting/apply" color="blue" size="lg">
            Test Application Form
          </Button>
          
          <Button component={Link} to="/vetting/status" color="green" size="lg">
            Test Status Page
          </Button>
          
          <Button component={Link} to="/vetting/reviewer" color="orange" size="lg">
            Test Reviewer Dashboard
          </Button>
        </Group>
        
        <Divider />
        
        <Button component={Link} to="/" variant="outline" color="gray">
          Back to Home
        </Button>
      </Stack>
    </Container>
  );
};