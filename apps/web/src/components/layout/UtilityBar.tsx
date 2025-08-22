import { Box, Group } from '@mantine/core';
import { Link } from 'react-router-dom';

/**
 * UtilityBar Component - Top utility navigation bar
 * Matches the exact wireframe design with dark midnight background
 * and taupe colored links with hover animations
 */
export const UtilityBar: React.FC = () => {
  return (
    <Box
      style={{
        background: 'var(--color-midnight)',
        padding: '12px 40px',
        fontSize: '13px',
        color: 'var(--color-taupe)',
        fontFamily: 'var(--font-heading)',
        textTransform: 'uppercase',
        letterSpacing: '1px',
      }}
    >
      <Group justify="flex-end" gap="var(--space-lg)">
        <Box
          component={Link}
          to="/incident-report"
          style={{
            color: 'var(--color-brass)',
            fontWeight: 600,
            textDecoration: 'none',
            transition: 'all 0.3s ease',
          }}
          className="utility-bar-link incident-link"
        >
          Report an Incident
        </Box>
        <Box
          component={Link}
          to="/private-lessons"
          style={{
            color: 'var(--color-taupe)',
            textDecoration: 'none',
            transition: 'all 0.3s ease',
          }}
          className="utility-bar-link"
        >
          Private Lessons
        </Box>
        <Box
          component={Link}
          to="/contact"
          style={{
            color: 'var(--color-taupe)',
            textDecoration: 'none',
            transition: 'all 0.3s ease',
          }}
          className="utility-bar-link"
        >
          Contact
        </Box>
      </Group>
    </Box>
  );
};