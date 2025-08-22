import React from 'react';
import { Container, Text, Title, Group, Button, Box } from '@mantine/core';
import { Link } from 'react-router-dom';
import { useIsAuthenticated } from '../../stores/authStore';

interface HeroSectionProps {
  /** Override default CTA buttons if needed */
  customActions?: React.ReactNode;
}

export const HeroSection: React.FC<HeroSectionProps> = ({ customActions }) => {
  const isAuthenticated = useIsAuthenticated();

  return (
    <Box
      component="section"
      className="hero-section"
      style={{
        background: 'linear-gradient(180deg, var(--color-midnight) 0%, var(--color-burgundy-dark) 100%)',
        color: 'var(--color-ivory)',
        padding: 'var(--space-3xl) 40px var(--space-2xl)',
        textAlign: 'center',
        position: 'relative',
        overflow: 'hidden',
        minHeight: '80vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      {/* Animated rope pattern overlay */}
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          opacity: 0.1,
          pointerEvents: 'none',
          backgroundImage: "url(\"data:image/svg+xml,%3Csvg width='400' height='400' viewBox='0 0 400 400' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M50,200 Q150,100 250,200 T450,200' stroke='%23B76D75' stroke-width='2' fill='none'/%3E%3Cpath d='M0,250 Q100,150 200,250 T400,250' stroke='%23B76D75' stroke-width='1.5' fill='none'/%3E%3C/svg%3E\")",
          backgroundSize: '800px 800px',
        }}
        className="hero-float-animation"
      />

      {/* Radial gradient overlay */}
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `radial-gradient(circle at 20% 80%, var(--color-rose-gold) 0%, transparent 50%),
                      radial-gradient(circle at 80% 20%, var(--color-plum) 0%, transparent 50%)`,
          opacity: 0.05,
          pointerEvents: 'none',
        }}
      />

      <Container size="lg" style={{ position: 'relative', zIndex: 1, maxWidth: 900 }}>
        <Text
          style={{
            fontFamily: 'var(--font-accent)',
            fontSize: '28px',
            color: 'var(--color-rose-gold)',
            marginBottom: 'var(--space-sm)',
            opacity: 0.9,
          }}
        >
          Where curiosity meets connection
        </Text>

        <Title
          order={1}
          style={{
            fontFamily: 'var(--font-heading)',
            fontSize: '64px',
            marginBottom: 'var(--space-md)',
            fontWeight: 800,
            lineHeight: 1.1,
            letterSpacing: '-1px',
            textTransform: 'uppercase',
          }}
        >
          Salem's Rope Bondage
          <br />
          <Text
            component="span"
            style={{
              background: 'linear-gradient(135deg, var(--color-rose-gold) 0%, var(--color-brass) 100%)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
            }}
          >
            Education & Practice
          </Text>
          <br />
          Community
        </Title>

        <Text
          size="xl"
          style={{
            fontFamily: 'var(--font-body)',
            fontSize: '22px',
            marginBottom: 'var(--space-xl)',
            opacity: 0.9,
            lineHeight: 1.6,
            fontWeight: 300,
          }}
        >
          Join 600+ members learning and growing together in a supportive, consent-focused space that celebrates every journey
        </Text>

        {customActions || (
          <Group justify="center" gap="md">
            <Button
              component={Link}
              to="#events"
              variant="v7-primary"
              size="lg"
              style={{
                padding: '18px 40px',
                fontSize: '16px',
              }}
            >
              Browse Upcoming Classes
            </Button>
            <Button
              component={Link}
              to={isAuthenticated ? "/welcome" : "#join"}
              variant="v7-secondary"
              size="lg"
              style={{
                padding: '18px 40px',
                fontSize: '16px',
                background: 'transparent',
                color: 'var(--color-ivory)',
                borderColor: 'var(--color-rose-gold)',
                '&::before': {
                  background: 'var(--color-rose-gold)',
                },
                '&:hover': {
                  color: 'var(--color-midnight)',
                }
              }}
            >
              {isAuthenticated ? 'Go to Dashboard' : 'Start Your Journey'}
            </Button>
          </Group>
        )}
      </Container>
    </Box>
  );
};