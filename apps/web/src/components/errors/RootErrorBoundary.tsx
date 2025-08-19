import { useRouteError, isRouteErrorResponse, Link } from 'react-router-dom';
import { Box, Title, Text, Button, Group, Paper } from '@mantine/core';

/**
 * Root Error Boundary for React Router v7
 * 
 * Handles different types of routing errors:
 * - HTTP status errors (404, 403, etc.)
 * - Generic JavaScript errors
 * - Network/API errors
 * 
 * Pattern from: /docs/functional-areas/routing-validation/requirements/functional-specification.md
 * Section 4.4 - Error handling patterns
 */
export const RootErrorBoundary: React.FC = () => {
  const error = useRouteError();

  // Handle HTTP status errors (404, 403, etc.)
  if (isRouteErrorResponse(error)) {
    return (
      <Box
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '2rem'
        }}
      >
        <Paper shadow="md" p="xl" style={{ maxWidth: '500px', textAlign: 'center' }}>
          <Title order={1} c="red" mb="md">
            {error.status} {error.statusText}
          </Title>
          
          <Text size="lg" mb="xl" c="dimmed">
            {error.data || 'Something went wrong'}
          </Text>

          <Group justify="center" gap="md">
            {error.status === 403 && (
              <Button
                component={Link}
                to="/login"
                variant="filled"
                color="blue"
              >
                Login Required
              </Button>
            )}
            
            {error.status === 404 && (
              <Button
                component={Link}
                to="/"
                variant="filled"
                color="violet"
              >
                Return Home
              </Button>
            )}
            
            <Button
              variant="outline"
              onClick={() => window.location.reload()}
            >
              Try Again
            </Button>
          </Group>
        </Paper>
      </Box>
    );
  }

  // Handle generic JavaScript errors
  const errorMessage = error instanceof Error 
    ? error.message 
    : 'An unexpected error occurred';

  const errorStack = error instanceof Error && error.stack
    ? error.stack
    : null;

  return (
    <Box
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '2rem'
      }}
    >
      <Paper shadow="md" p="xl" style={{ maxWidth: '600px', textAlign: 'center' }}>
        <Title order={1} c="red" mb="md">
          Something went wrong
        </Title>
        
        <Text size="lg" mb="md" c="dimmed">
          {errorMessage}
        </Text>
        
        {process.env.NODE_ENV === 'development' && errorStack && (
          <Box
            style={{
              backgroundColor: 'var(--mantine-color-dark-6)',
              padding: '1rem',
              borderRadius: '4px',
              marginBottom: '1rem',
              textAlign: 'left',
              fontFamily: 'monospace',
              fontSize: '0.8rem',
              overflow: 'auto',
              maxHeight: '200px'
            }}
          >
            <Text size="xs" c="red">
              {errorStack}
            </Text>
          </Box>
        )}

        <Group justify="center" gap="md">
          <Button
            component={Link}
            to="/"
            variant="filled"
            color="violet"
          >
            Return Home
          </Button>
          
          <Button
            variant="outline"
            onClick={() => window.location.reload()}
          >
            Reload Page
          </Button>
        </Group>
      </Paper>
    </Box>
  );
};