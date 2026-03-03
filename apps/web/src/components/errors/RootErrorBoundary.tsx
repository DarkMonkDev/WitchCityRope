import { useEffect } from 'react';
import { useRouteError, isRouteErrorResponse, Link } from 'react-router-dom';
import { Box, Title, Text, Button, Group, Paper } from '@mantine/core';
import { NotFoundPage } from '../../pages/NotFoundPage';
import { reportError } from '../../lib/errorReporting';

/**
 * Root Error Boundary for React Router v7
 *
 * Handles different types of routing errors:
 * - HTTP 404 errors → renders branded NotFoundPage
 * - HTTP 403 errors → shows login prompt
 * - Other HTTP errors → shows status code and message
 * - Generic JavaScript errors → shows error details (stack trace in dev only)
 * - Network/API errors → shows generic error message
 *
 * This component is used as `errorElement` in the router config, which means
 * it renders within the router context and has access to useNavigate() and
 * other router hooks. NotFoundPage can safely use useNavigate() here.
 *
 * Pattern from: /docs/functional-areas/routing-validation/requirements/functional-specification.md
 * Section 4.4 - Error handling patterns
 */
export const RootErrorBoundary: React.FC = () => {
  const error = useRouteError();

  useEffect(() => {
    if (error instanceof Error) {
      reportError({
        message: error.message,
        stack: error.stack,
        type: 'react_error',
        url: window.location.href,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
      });
    } else if (isRouteErrorResponse(error)) {
      reportError({
        message: `${error.status} ${error.statusText}`,
        type: 'react_error',
        url: window.location.href,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
        metadata: { status: error.status, data: error.data },
      });
    }
  }, [error]);

  // Handle HTTP status errors (404, 403, etc.)
  if (isRouteErrorResponse(error)) {
    // For 404 errors, render the branded NotFoundPage component
    // instead of the generic error UI. This provides a consistent
    // 404 experience regardless of whether the error comes from
    // a route-level throw or an unmatched URL pattern.
    if (error.status === 404) {
      return <NotFoundPage />;
    }

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
