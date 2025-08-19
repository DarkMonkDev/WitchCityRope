import { Link, useNavigate } from 'react-router-dom';
import { Group, Button, Text, Box } from '@mantine/core';
import { useAuth, useAuthActions } from '../../stores/authStore';

/**
 * Navigation Component using Mantine components
 * Integrated with Zustand auth store for authentication state
 */
export const Navigation: React.FC = () => {
  const { user, isAuthenticated } = useAuth();
  const { logout } = useAuthActions();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      logout();
      navigate('/');
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  return (
    <Box
      style={{
        height: '100%',
        padding: '0 1rem',
        display: 'flex',
        alignItems: 'center',
        backgroundColor: 'var(--mantine-color-dark-8)',
        borderBottom: '1px solid var(--mantine-color-dark-4)'
      }}
    >
      <Group justify="space-between" style={{ width: '100%' }}>
        <Group>
          <Text
            component={Link}
            to="/"
            size="xl"
            fw={700}
            c="violet"
            style={{ textDecoration: 'none' }}
          >
            WitchCityRope
          </Text>
          
          <Text component={Link} to="/" c="dimmed" style={{ textDecoration: 'none' }}>
            Events
          </Text>
          
          <Text component={Link} to="/form-test" c="dimmed" style={{ textDecoration: 'none' }}>
            Form Test
          </Text>
          
          <Text component={Link} to="/mantine-forms" c="dimmed" style={{ textDecoration: 'none' }}>
            Mantine Forms
          </Text>
          
          <Text component={Link} to="/api-validation-v2-simple" c="dimmed" style={{ textDecoration: 'none' }}>
            API Demo
          </Text>
        </Group>

        <Group>
          {isAuthenticated && user ? (
            <>
              <Text component={Link} to="/dashboard" c="dimmed" style={{ textDecoration: 'none' }}>
                Welcome, {user.sceneName}
              </Text>
              <Button
                variant="filled"
                color="red"
                size="sm"
                onClick={handleLogout}
              >
                Logout
              </Button>
            </>
          ) : (
            <>
              <Text component={Link} to="/login" c="dimmed" style={{ textDecoration: 'none' }}>
                Login
              </Text>
              <Button
                component={Link}
                to="/register"
                variant="filled"
                color="violet"
                size="sm"
              >
                Register
              </Button>
            </>
          )}
        </Group>
      </Group>
    </Box>
  );
};