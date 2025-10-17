// Vetting application status check page
import React, { useState } from 'react';
import {
  Container,
  Box,
  Paper,
  Text,
  Button,
  Group,
  Alert,
  Stack,
  Title,
  TextInput,
  Divider,
  Anchor
} from '@mantine/core';
import {
  IconSearch,
  IconInfoCircle,
  IconAlertTriangle,
  IconMail,
  IconShieldCheck
} from '@tabler/icons-react';
import { ApplicationStatusComponent } from '../components/status/ApplicationStatus';
import { useStatusPageParams } from '../hooks/useApplicationStatus';
import { Link } from 'react-router-dom';

interface VettingStatusPageProps {
  className?: string;
}

export const VettingStatusPage: React.FC<VettingStatusPageProps> = ({
  className
}) => {
  const [inputToken, setInputToken] = useState('');
  const [currentToken, setCurrentToken] = useState<string | undefined>();
  const [tokenNotFound, setTokenNotFound] = useState(false);

  const { getTrackingToken, clearStoredToken, updateUrlWithToken } = useStatusPageParams();

  // Check for existing token on component mount
  React.useEffect(() => {
    const existingToken = getTrackingToken();
    if (existingToken) {
      setCurrentToken(existingToken);
      setInputToken(existingToken);
    }
  }, [getTrackingToken]);

  const handleTokenSubmit = () => {
    const token = inputToken.trim();
    if (token) {
      setCurrentToken(token);
      setTokenNotFound(false);
      updateUrlWithToken(token);
      localStorage.setItem('vetting-status-token', token);
    }
  };

  const handleTokenNotFound = () => {
    setTokenNotFound(true);
    setCurrentToken(undefined);
    clearStoredToken();
  };

  const handleClearToken = () => {
    setCurrentToken(undefined);
    setInputToken('');
    setTokenNotFound(false);
    clearStoredToken();
    
    // Clear URL parameters
    if (typeof window !== 'undefined') {
      const url = new URL(window.location.href);
      url.searchParams.delete('token');
      window.history.replaceState({}, '', url.toString());
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleTokenSubmit();
    }
  };

  return (
    <Container size="md" py="xl" className={className}>
      {/* Page Header */}
      <Paper p="xl" shadow="sm" mb="xl" ta="center">
        <Stack gap="lg">
          <Box>
            <Title order={1} size="h1" c="wcr.7" mb="md">
              Check Application Status
            </Title>
            <Text size="lg" c="dimmed" mx="auto" style={{ maxWidth: 600 }}>
              Enter your tracking token or application number to check the status of your 
              vetting application.
            </Text>
          </Box>
        </Stack>
      </Paper>

      {/* Token Input Section */}
      {!currentToken && (
        <Paper p="xl" shadow="sm" mb="xl">
          <Stack gap="lg">
            <Box>
              <Text size="lg" fw={600} c="wcr.7" mb="md">
                Enter Your Tracking Information
              </Text>
              <Text size="sm" c="dimmed" mb="lg">
                You can find your tracking token in the confirmation email we sent when you 
                submitted your application, or use your application number.
              </Text>
            </Box>

            <Group align="flex-end">
              <TextInput
                label="Tracking Token or Application Number"
                placeholder="e.g., VET-2024-001234 or your tracking token"
                value={inputToken}
                onChange={(e) => setInputToken(e.target.value)}
                onKeyPress={handleKeyPress}
                leftSection={<IconSearch size={16} />}
                style={{ flex: 1 }}
                size="lg"
                error={tokenNotFound ? 'Application not found with this token' : undefined}
              />
              <Button
                onClick={handleTokenSubmit}
                disabled={!inputToken.trim()}
                color="wcr.7"
                size="lg"
                style={{
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600
                }}
              >
                Check Status
              </Button>
            </Group>

            {tokenNotFound && (
              <Alert
                icon={<IconAlertTriangle />}
                color="red"
                title="Application Not Found"
              >
                <Text size="sm" mb="md">
                  We couldn't find an application with that tracking token or number. 
                  Please check that you've entered it correctly.
                </Text>
                
                <Text size="sm" mb="md">
                  If you continue to have problems:
                </Text>
                
                <Text size="sm" component="ul" pl="md">
                  <li>Check your confirmation email for the correct tracking token</li>
                  <li>Make sure you're using the full application number (e.g., VET-2024-001234)</li>
                  <li>Contact our support team if you can't locate your tracking information</li>
                </Text>
              </Alert>
            )}
          </Stack>
        </Paper>
      )}

      {/* Application Status Display */}
      {currentToken && !tokenNotFound && (
        <>
          {/* Status Actions */}
          <Paper p="md" mb="lg" withBorder>
            <Group justify="apart">
              <Group gap="xs">
                <Text size="sm" c="dimmed">
                  Viewing status for: <Text component="span" fw={500}>{currentToken}</Text>
                </Text>
              </Group>
              <Button
                variant="subtle"
                size="xs"
                onClick={handleClearToken}
              >
                Check Different Application
              </Button>
            </Group>
          </Paper>

          {/* Status Component */}
          <ApplicationStatusComponent
            trackingToken={currentToken}
            onTokenNotFound={handleTokenNotFound}
          />
        </>
      )}

      {/* Help Section */}
      <Paper p="xl" bg="blue.0" withBorder>
        <Stack gap="md">
          <Group gap="md">
            <IconInfoCircle size={24} color="#4285F4" />
            <Text size="lg" fw={600} c="blue.7">
              Don't have your tracking information?
            </Text>
          </Group>
          
          <Text size="sm" c="blue.7" mb="md">
            If you can't find your tracking token or application number, here are some options:
          </Text>
          
          <Stack gap="sm">
            <Group gap="xs" align="flex-start">
              <IconMail size={16} color="#4285F4" style={{ marginTop: 2 }} />
              <Text size="sm" c="blue.7">
                <strong>Check your email:</strong> Look for the confirmation email from WitchCityRope 
                with subject "Application Received - Confirmation and Tracking Information"
              </Text>
            </Group>
            
            <Group gap="xs" align="flex-start">
              <IconShieldCheck size={16} color="#4285F4" style={{ marginTop: 2 }} />
              <Text size="sm" c="blue.7">
                <strong>Contact support:</strong> Our team can help you locate your application 
                using your email address or other identifying information
              </Text>
            </Group>
          </Stack>
          
          <Divider />
          
          <Group>
            <Button variant="light" color="blue" size="sm" component={Link} to="/contact-us">
              Contact Support
            </Button>
            <Button variant="subtle" color="blue" size="sm" component={Link} to="/vetting/apply">
              Submit New Application
            </Button>
          </Group>
        </Stack>
      </Paper>

      {/* Additional Information */}
      <Paper p="lg" bg="gray.0" withBorder>
        <Stack gap="md">
          <Text size="md" fw={600} c="wcr.7">
            About the Vetting Process
          </Text>
          
          <Text size="sm" c="dimmed">
            Our vetting process typically takes 5-14 business days, depending on reference response 
            times and application complexity. You'll receive email notifications at key milestones, 
            and you can check this page anytime for updates.
          </Text>
          
          <Group>
            <Anchor component={Link} to="/vetting/faq" size="sm">
              Vetting FAQ
            </Anchor>
            <Anchor component={Link} to="/community/guidelines" size="sm">
              Community Guidelines
            </Anchor>
            <Anchor component={Link} to="/about" size="sm">
              About WitchCityRope
            </Anchor>
          </Group>
        </Stack>
      </Paper>
    </Container>
  );
};