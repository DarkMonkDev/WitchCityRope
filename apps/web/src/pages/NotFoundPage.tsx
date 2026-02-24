import React from 'react'
import { useNavigate, Link } from 'react-router-dom'
import {
  Container,
  Title,
  Text,
  Button,
  Paper,
  Stack,
  Group,
  SimpleGrid,
  Box,
  Anchor,
} from '@mantine/core'
import {
  IconHome,
  IconArrowLeft,
  IconCalendarEvent,
  IconUsers,
  IconUserPlus,
  IconLayoutDashboard,
} from '@tabler/icons-react'

/**
 * NotFoundPage (HTTP 404)
 *
 * Branded 404 error page for WitchCityRope, matching the design wireframe
 * at docs/design/wireframes/error-404-visual.html.
 *
 * Used in three places:
 * 1. Router catch-all route (path: '*') for multi-segment unknown URLs
 * 2. CmsDynamicPage when the CMS API returns 404 for a non-existent slug
 * 3. RootErrorBoundary when a route-level 404 error is caught
 *
 * Design notes:
 * - Burgundy/plum gradient on the 404 number matches WitchCityRope brand
 * - Rope-themed messaging ("That Knot Doesn't Exist") ties into community identity
 * - Quick links help users find what they were likely looking for
 * - Go Back button uses navigate(-1) for browser history integration
 */
export const NotFoundPage: React.FC = () => {
  const navigate = useNavigate()

  const handleGoHome = () => {
    navigate('/')
  }

  const handleGoBack = () => {
    navigate(-1)
  }

  return (
    <Container size="sm" py="xl">
      <Paper
        shadow="md"
        p="xl"
        radius="md"
        withBorder
        style={{ textAlign: 'center' }}
      >
        <Stack gap="md" align="center">
          {/* Large 404 number with WitchCityRope brand gradient */}
          <Title
            order={1}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '120px',
              fontWeight: 800,
              lineHeight: 1,
              background: 'linear-gradient(135deg, #880124 0%, #614B79 100%)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              backgroundClip: 'text',
            }}
          >
            404
          </Title>

          {/* Rope-themed heading matching community identity */}
          <Title
            order={2}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '24px',
              fontWeight: 700,
              color: 'var(--mantine-color-dark-6)',
            }}
          >
            Oops! That Knot Doesn&apos;t Exist
          </Title>

          <Text size="lg" c="dimmed" maw={450}>
            Looks like this rope has come untied. The page you&apos;re looking
            for might have been moved, renamed, or is temporarily unavailable.
          </Text>

          {/* Decorative rope-inspired SVG divider */}
          <Box
            style={{
              width: '200px',
              height: '40px',
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='200' height='40' viewBox='0 0 200 40' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M0,20 Q50,5 100,20 T200,20' stroke='%23880124' stroke-width='2.5' fill='none'/%3E%3Cpath d='M0,20 Q50,35 100,20 T200,20' stroke='%23614B79' stroke-width='1.5' fill='none' opacity='0.6'/%3E%3C/svg%3E")`,
              backgroundRepeat: 'no-repeat',
              backgroundPosition: 'center',
            }}
          />

          {/* Primary action buttons */}
          <Group gap="md" mt="sm">
            <Button
              leftSection={<IconArrowLeft size={16} />}
              variant="outline"
              color="gray"
              onClick={handleGoBack}
              size="md"
              styles={{
                root: {
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '1px',
                  fontSize: '13px',
                },
              }}
            >
              Go Back
            </Button>

            <Button
              leftSection={<IconHome size={16} />}
              variant="filled"
              color="dark"
              onClick={handleGoHome}
              size="md"
              styles={{
                root: {
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '1px',
                  fontSize: '13px',
                  background:
                    'linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)',
                  color: '#1A1A2E',
                },
              }}
            >
              Go Home
            </Button>
          </Group>

          {/* Quick links section — helps users find common destinations */}
          <Box
            mt="lg"
            pt="lg"
            style={{
              borderTop: '1px solid var(--mantine-color-gray-3)',
              width: '100%',
            }}
          >
            <Text fw={600} mb="md" size="sm" tt="uppercase" style={{ letterSpacing: '1px' }}>
              Popular Pages
            </Text>

            <SimpleGrid cols={{ base: 1, xs: 2 }} spacing="xs">
              <Anchor
                component={Link}
                to="/events"
                underline="hover"
                c="dark.6"
                style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
              >
                <IconCalendarEvent size={16} color="#880124" />
                Browse Events & Classes
              </Anchor>

              <Anchor
                component={Link}
                to="/join"
                underline="hover"
                c="dark.6"
                style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
              >
                <IconUserPlus size={16} color="#880124" />
                Become a Member
              </Anchor>

              <Anchor
                component={Link}
                to="/events"
                underline="hover"
                c="dark.6"
                style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
              >
                <IconUsers size={16} color="#880124" />
                Upcoming Meetups
              </Anchor>

              <Anchor
                component={Link}
                to="/dashboard"
                underline="hover"
                c="dark.6"
                style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
              >
                <IconLayoutDashboard size={16} color="#880124" />
                My Dashboard
              </Anchor>
            </SimpleGrid>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}
