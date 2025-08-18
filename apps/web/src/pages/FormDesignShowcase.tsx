import React from 'react';
import { Container, Grid, Card, Text, Button, Box, Group, Badge } from '@mantine/core';
import { Link } from 'react-router-dom';
import { IconEye, IconSparkles } from '@tabler/icons-react';

const FormDesignShowcase: React.FC = () => {
  const designs = [
    {
      id: 'a',
      title: 'Floating Labels',
      subtitle: 'Modern & Elegant',
      description: 'Sophisticated floating label design with smooth transitions, subtle lighting effects, and elevation on focus',
      icon: <IconSparkles size={24} />,
      features: ['Smooth animations', 'Elevation on focus', 'Helper text support', 'Professional feel'],
      color: 'violet',
      gradient: 'linear-gradient(135deg, #9b4a75 0%, #b47171 100%)',
      route: '/form-designs/a'
    },
    {
      id: 'b',
      title: 'Floating Label with Underline',
      subtitle: 'Clean Focus Indicator',
      description: 'Elegant floating labels with clean underline animation on focus. Same sophistication as Design A but with minimal, understated focus indicator instead of elevation effects.',
      icon: <IconEye size={24} />,
      features: ['Floating label animation', 'Clean underline effect', 'Helper text support', 'Minimalist focus indicator'],
      color: 'blue',
      gradient: 'linear-gradient(135deg, #d4a5a5 0%, #c48b8b 100%)',
      route: '/form-designs/b'
    }
    // Designs C and D hidden per user preference
    // {
    //   id: 'c',
    //   title: '3D Elevation',
    //   subtitle: 'Premium & Tactile',
    //   description: 'Fields physically lift off the page when focused, creating a premium tactile experience with multi-layered shadows',
    //   icon: <IconCube size={24} />,
    //   features: ['Physical elevation effect', '4-layer shadow system', 'Subtle 3D rotation', 'Surrounding field dimming'],
    //   color: 'grape',
    //   gradient: 'linear-gradient(135deg, #9b4a75 0%, #880124 100%)',
    //   route: '/form-designs/c'
    // },
    // {
    //   id: 'd',
    //   title: 'Neon Ripple Spotlight',
    //   subtitle: 'Cyberpunk & Dynamic',
    //   description: 'Cyberpunk-inspired with pulsing neon glows, click-position ripple effects, and dynamic spotlighting',
    //   icon: <IconBolt size={24} />,
    //   features: ['Pulsing neon glow', 'Click position ripples', 'Dynamic spotlight', 'Cyberpunk aesthetic'],
    //   color: 'pink',
    //   gradient: 'linear-gradient(135deg, #880124 0%, #6b0119 100%)',
    //   route: '/form-designs/d'
    // }
  ];

  return (
    <Container size="lg" py="xl">
      <Box mb="xl" style={{ textAlign: 'center' }}>
        <Text
          size="xl"
          fw={700}
          mb="md"
          style={{
            background: 'linear-gradient(135deg, #9b4a75 0%, #b47171 50%, #d4a5a5 100%)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            fontSize: '48px',
            fontFamily: 'Bodoni Moda, serif',
            letterSpacing: '1px'
          }}
        >
          Form Design Gallery
        </Text>
        <Text size="lg" c="dimmed" maw={600} mx="auto">
          Explore our two recommended form designs, each offering a different approach to floating labels and user interaction for the WitchCityRope platform.
        </Text>
      </Box>

      <Grid justify="center">
        {designs.map((design) => (
          <Grid.Col key={design.id} span={{ base: 12, sm: 10, md: 6, lg: 5 }}>
            <Card
              shadow="lg"
              p="xl"
              radius="lg"
              h="100%"
              style={{
                background: 'linear-gradient(145deg, rgba(44, 44, 44, 0.8) 0%, rgba(26, 26, 26, 0.9) 100%)',
                backdropFilter: 'blur(20px)',
                border: '1px solid rgba(212, 165, 165, 0.1)',
                transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
                cursor: 'pointer',
                position: 'relative',
                overflow: 'hidden'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = 'translateY(-4px)';
                e.currentTarget.style.boxShadow = '0 20px 40px rgba(0, 0, 0, 0.3)';
                e.currentTarget.style.borderColor = 'rgba(155, 74, 117, 0.3)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = 'translateY(0)';
                e.currentTarget.style.boxShadow = '0 8px 15px rgba(0, 0, 0, 0.1)';
                e.currentTarget.style.borderColor = 'rgba(212, 165, 165, 0.1)';
              }}
            >
              {/* Gradient accent bar */}
              <Box
                style={{
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  right: 0,
                  height: '4px',
                  background: design.gradient
                }}
              />

              <Group mb="md" gap="md">
                <Box
                  style={{
                    background: design.gradient,
                    borderRadius: '12px',
                    padding: '12px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: 'white'
                  }}
                >
                  {design.icon}
                </Box>
                <Box style={{ flex: 1 }}>
                  <Text size="xl" fw={600} c="white" mb={4}>
                    {design.title}
                  </Text>
                  <Badge
                    variant="light"
                    color={design.color}
                    size="sm"
                  >
                    {design.subtitle}
                  </Badge>
                </Box>
              </Group>

              <Text c="dimmed" mb="md" style={{ lineHeight: 1.6 }}>
                {design.description}
              </Text>

              <Box mb="lg">
                <Text size="sm" fw={500} c="white" mb="xs">Key Features:</Text>
                <Box>
                  {design.features.map((feature, index) => (
                    <Text key={index} size="sm" c="dimmed" mb={2}>
                      • {feature}
                    </Text>
                  ))}
                </Box>
              </Box>

              <Button
                component={Link}
                to={design.route}
                variant="gradient"
                gradient={{ from: 'grape.6', to: 'grape.7', deg: 45 }}
                fullWidth
                size="md"
                style={{
                  height: '48px',
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px'
                }}
              >
                View Design
              </Button>
            </Card>
          </Grid.Col>
        ))}
      </Grid>

      <Box mt="xl" p="xl" style={{
        background: 'linear-gradient(145deg, rgba(44, 44, 44, 0.6) 0%, rgba(26, 26, 26, 0.8) 100%)',
        borderRadius: '12px',
        border: '1px solid rgba(212, 165, 165, 0.1)',
        textAlign: 'center'
      }}>
        <Text size="lg" fw={500} c="white" mb="md">
          Two Approaches to Floating Labels
        </Text>
        <Text c="dimmed" maw={800} mx="auto" style={{ lineHeight: 1.6 }}>
          Both designs use elegant floating label animation but with different focus indicators. 
          Choose elevation effects for premium feel or clean underlines for minimal sophistication.
        </Text>
        <Group justify="center" mt="lg">
          <Button
            component={Link}
            to="/"
            variant="outline"
            c="white"
            style={{ borderColor: 'rgba(212, 165, 165, 0.3)' }}
          >
            Back to Home
          </Button>
        </Group>
      </Box>
    </Container>
  );
};

export default FormDesignShowcase;