/**
 * Navigation Test Page - Test Navigation component for reload issues
 * 
 * This page includes the Navigation component and tracks mount/unmount
 * to verify that the Navigation component fix is working properly.
 * 
 * @created 2025-09-06
 */

import React, { useState, useEffect } from 'react';
import { Container, Title, Text, Paper, Button, Box } from '@mantine/core';

export const NavigationTestPage: React.FC = () => {
  const [mountCount, setMountCount] = useState(0);
  const [renderCount, setRenderCount] = useState(0);

  // Track component mounts
  useEffect(() => {
    console.log('🧪 NavigationTestPage mounted');
    setMountCount(prev => prev + 1);
    
    return () => {
      console.log('🧪 NavigationTestPage unmounted');
    };
  }, []);

  // Track renders
  useEffect(() => {
    setRenderCount(prev => prev + 1);
    console.log('🧪 NavigationTestPage render count:', renderCount + 1);
  });

  return (
    <Container size="md" py="xl">
      <Title order={1} mb="xl">
        Navigation Component Test Page
      </Title>

      <Paper p="md" mb="md" withBorder>
        <Title order={3} mb="md">
          Component Lifecycle Tracking
        </Title>
        <Text mb="sm">
          <strong>Mount Count:</strong> {mountCount}
        </Text>
        <Text mb="sm">
          <strong>Render Count:</strong> {renderCount}
        </Text>
        <Text size="sm" c="dimmed">
          If Navigation is causing constant reloads, you'll see these numbers 
          constantly increasing. With the fix applied, mount count should be 
          2 (React.StrictMode) and render count should stabilize.
        </Text>
      </Paper>

      <Paper p="md" mb="md" withBorder>
        <Title order={3} mb="md">
          Test Results
        </Title>
        <Text mb="sm">
          ✅ <strong>Expected Behavior:</strong>
        </Text>
        <ul>
          <li>Mount count: 2 (due to React.StrictMode double-mounting)</li>
          <li>Render count: Should stabilize after initial renders</li>
          <li>No console errors about auth loops</li>
          <li>Page remains stable and doesn't reload</li>
        </ul>
        
        <Text mb="sm" mt="md">
          ❌ <strong>Problem Behavior (if fix didn't work):</strong>
        </Text>
        <ul>
          <li>Mount/render counts constantly increasing</li>
          <li>Console showing repeated auth checks</li>
          <li>Page visibly reloading/flickering</li>
        </ul>
      </Paper>

      <Paper p="md" withBorder>
        <Title order={3} mb="md">
          Manual Testing
        </Title>
        <Text mb="md">
          Click the button below to trigger a state change. If the fix is working,
          the page should remain stable.
        </Text>
        <Button 
          onClick={() => {
            console.log('🧪 Manual button click - testing stability');
            setRenderCount(prev => prev + 1);
          }}
        >
          Test State Change
        </Button>
      </Paper>

      <Box mt="xl" c="dimmed" size="sm">
        <Text>
          This page includes the full Navigation component via RootLayout.
          If you can read this message and the counters are stable, the fix is working!
        </Text>
      </Box>
    </Container>
  );
};