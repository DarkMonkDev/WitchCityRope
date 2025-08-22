import React from 'react';
import { Box, Container, SimpleGrid, Text, Title } from '@mantine/core';

interface Feature {
  icon: string;
  title: string;
  description: string;
}

interface FeatureGridProps {
  /** Section title */
  title?: string;
  /** Array of features to display */
  features?: Feature[];
}

const defaultFeatures: Feature[] = [
  {
    icon: '🎭',
    title: 'Expert Teaching',
    description: 'Learn from experienced instructors who make rope accessible to everyone. We blend technical skills with creativity and always prioritize safety.'
  },
  {
    icon: '🌹',
    title: 'Welcoming Community',
    description: 'Find your people in our warm, respectful space. We celebrate diversity, honor boundaries, and support each other\'s growth.'
  },
  {
    icon: '🔮',
    title: 'Safety First, Always',
    description: 'We\'re committed to risk-aware practice, ongoing consent, and creating an environment where everyone feels secure to learn and explore.'
  },
  {
    icon: '✨',
    title: 'Everyone Belongs',
    description: 'All bodies, orientations, and experience levels are welcome here. Come as you are—we\'re excited to support your unique journey.'
  }
];

export const FeatureGrid: React.FC<FeatureGridProps> = ({
  title = "What Makes Our Community Special",
  features = defaultFeatures
}) => {
  return (
    <Box
      component="section"
      style={{
        padding: 'var(--space-2xl) 40px',
        maxWidth: '1200px',
        margin: '0 auto',
        position: 'relative',
      }}
    >
      <Title
        order={2}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '48px',
          fontWeight: 800,
          marginBottom: 'var(--space-xl)',
          textAlign: 'center',
          color: 'var(--color-burgundy)',
          position: 'relative',
          textTransform: 'uppercase',
          letterSpacing: '3px',
        }}
      >
        {title}
        <Box
          style={{
            content: '""',
            display: 'block',
            width: '100px',
            height: '2px',
            background: 'linear-gradient(90deg, transparent, var(--color-rose-gold), transparent)',
            margin: 'var(--space-sm) auto 0',
          }}
        />
      </Title>

      <SimpleGrid
        cols={{ base: 1, sm: 2, lg: 4 }}
        spacing="xl"
        style={{ marginBottom: 'var(--space-xl)' }}
      >
        {features.map((feature, index) => (
          <Box
            key={index}
            style={{
              textAlign: 'center',
              position: 'relative',
            }}
          >
            <Box
              className="feature-icon-animation"
              style={{
                cursor: 'pointer',
              }}
            >
              {feature.icon}
            </Box>

            <Title
              order={3}
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '24px',
                marginBottom: 'var(--space-sm)',
                color: 'var(--color-burgundy)',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              {feature.title}
            </Title>

            <Text
              style={{
                color: 'var(--color-charcoal)',
                lineHeight: 1.7,
                fontSize: '16px',
              }}
            >
              {feature.description}
            </Text>
          </Box>
        ))}
      </SimpleGrid>
    </Box>
  );
};