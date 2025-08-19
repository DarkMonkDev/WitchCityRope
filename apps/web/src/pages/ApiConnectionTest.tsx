import React, { useState } from 'react';
import { Box, Button, Card, Text, TextInput, Stack, Alert, Code } from '@mantine/core';
import { api } from '../api/client';

/**
 * API Connection Test Page
 * 
 * This page verifies that:
 * 1. MSW is disabled (we get real API responses)
 * 2. React app connects to real API at localhost:5653
 * 3. Real API validation and error responses work
 */
export function ApiConnectionTest() {
  const [results, setResults] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [email, setEmail] = useState('admin@witchcityrope.com');
  const [password, setPassword] = useState('Test123');

  const addResult = (message: string) => {
    setResults(prev => [...prev, `${new Date().toLocaleTimeString()}: ${message}`]);
  };

  const testApiConnection = async () => {
    setLoading(true);
    setResults([]);
    
    try {
      // Test 1: Health endpoint
      addResult('Testing API health endpoint...');
      const healthResponse = await api.get('/api/Health');
      addResult(`✅ Health check: ${healthResponse.status} - ${healthResponse.data}`);
    } catch (error: any) {
      addResult(`❌ Health check failed: ${error.message}`);
    }

    try {
      // Test 2: Protected endpoint (should return 401)
      addResult('Testing protected endpoint (should return 401)...');
      await api.get('/api/Protected/profile');
      addResult('❌ Protected endpoint should have returned 401');
    } catch (error: any) {
      if (error.response?.status === 401) {
        addResult('✅ Protected endpoint correctly returned 401 (not authenticated)');
      } else {
        addResult(`❌ Protected endpoint unexpected error: ${error.message}`);
      }
    }

    try {
      // Test 3: Login with test credentials (should fail with real API validation)
      addResult('Testing login with test credentials...');
      await api.post('/api/Auth/login', { email, password });
      addResult('❌ Login should have failed (test user not in real DB)');
    } catch (error: any) {
      if (error.response?.status === 401) {
        const errorData = error.response.data;
        addResult('✅ Login correctly failed with real API validation');
        addResult(`   Response: ${JSON.stringify(errorData, null, 2)}`);
      } else {
        addResult(`❌ Login unexpected error: ${error.message}`);
      }
    }

    setLoading(false);
  };

  const testMswStatus = () => {
    addResult('=== MSW Status Check ===');
    
    // Check if we're getting mock responses or real API responses
    // MSW handlers return very specific mock data structure
    if (window.location.hostname === 'localhost') {
      addResult('✅ Running on localhost - real API expected');
    }
    
    addResult(`API Base URL: ${api.defaults.baseURL}`);
    addResult('If MSW is disabled, all requests above should hit the real API at localhost:5653');
    addResult('If MSW is enabled, responses would be from the mock handlers');
  };

  return (
    <Box p="md" maw={800} mx="auto">
      <Card shadow="sm" padding="lg" radius="md" withBorder>
        <Text size="xl" fw={700} mb="md">
          API Connection Test - MSW Disable Verification
        </Text>
        
        <Text mb="md" c="dimmed">
          This page tests if MSW is disabled and the app connects to the real API.
        </Text>

        <Stack mb="md">
          <TextInput
            label="Test Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="admin@witchcityrope.com"
          />
          <TextInput
            label="Test Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Test123"
          />
        </Stack>

        <Stack gap="sm" mb="md">
          <Button onClick={testApiConnection} loading={loading} color="blue">
            Test Real API Connection
          </Button>
          
          <Button onClick={testMswStatus} variant="outline" color="gray">
            Check MSW Status
          </Button>
        </Stack>

        {results.length > 0 && (
          <Card shadow="xs" padding="md" radius="sm" bg="gray.0">
            <Text fw={700} mb="sm">Test Results:</Text>
            <Stack gap="xs">
              {results.map((result, index) => (
                <Code key={index} block>
                  {result}
                </Code>
              ))}
            </Stack>
          </Card>
        )}

        <Alert color="blue" mt="md">
          <Text fw={700}>Expected Results When MSW is Disabled:</Text>
          <Text size="sm">
            • Health check should succeed<br/>
            • Protected endpoint should return 401 (Unauthorized)<br/>
            • Login should fail with real API validation error<br/>
            • All responses should come from localhost:5653 (real API)
          </Text>
        </Alert>
      </Card>
    </Box>
  );
}

export default ApiConnectionTest;