/**
 * MINIMAL VERSION - Events Management API Demo
 * This is a completely static version to test if the reloading issue persists
 * @created 2025-09-06
 */

import React, { useState, useEffect } from 'react';
import { 
  Container, 
  Title, 
  Paper, 
  Button, 
  Text, 
  Stack,
  Alert
} from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';

export const EventsManagementApiDemoMinimal: React.FC = () => {
  const [renderCount, setRenderCount] = useState(0);
  const [mountTime] = useState(() => new Date().toLocaleTimeString());

  // Track renders and mounts
  useEffect(() => {
    setRenderCount(prev => prev + 1);
  });

  useEffect(() => {
    console.log('🟢 MINIMAL: Component mounted at', mountTime);
    return () => {
      console.log('🔴 MINIMAL: Component unmounted');
    };
  }, [mountTime]);

  console.log('🔵 MINIMAL: Component rendering, count:', renderCount);

  return (
    <Container size="xl" py="xl">
      <Title order={1} ta="center" mb="xl" c="burgundy">
        MINIMAL TEST - Events Management API Integration Demo
      </Title>

      <Stack gap="xl">
        <Paper shadow="sm" radius="md" p="lg">
          <Title order={2} c="burgundy" mb="md">Debug Information</Title>
          <Stack gap="md">
            <Alert icon={<IconInfoCircle />} color="blue">
              <Text><strong>Component Status:</strong> This is a minimal static version to test for reloading issues</Text>
            </Alert>
            <Text><strong>Mount Time:</strong> {mountTime}</Text>
            <Text><strong>Render Count:</strong> {renderCount}</Text>
            <Button onClick={() => setRenderCount(prev => prev + 1)}>
              Force Re-render
            </Button>
          </Stack>
        </Paper>

        <Paper shadow="sm" radius="md" p="lg">
          <Title order={3} mb="md">Static Content Test</Title>
          <Stack gap="md">
            <Text>This component has no API calls, no complex hooks, no imports from problematic modules.</Text>
            <Text>If this page still reloads constantly, the issue is NOT in the component itself.</Text>
            <Text>If this page is stable, we can gradually add back features to isolate the problem.</Text>
          </Stack>
        </Paper>
      </Stack>
    </Container>
  );
};