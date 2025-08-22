import React from 'react';
import { Box, Container, Text, Title, Button } from '@mantine/core';
import { Link } from 'react-router-dom';

interface CTASectionProps {
  /** Main heading text */
  title?: string;
  /** Subtitle text above the title */
  subtitle?: string;
  /** Description text below the title */
  description?: string;
  /** Button text */
  buttonText?: string;
  /** Button link destination */
  buttonLink?: string;
  /** Custom button action component */
  customButton?: React.ReactNode;
}

export const CTASection: React.FC<CTASectionProps> = ({
  title = "Your Rope Journey Begins Here",
  subtitle = "Ready to start?",
  description = "Whether you're curious about rope or ready to deepen your practice, we're here to support you every step of the way.",
  buttonText = "Join Our Community",
  buttonLink = "#join",
  customButton
}) => {
  return (
    <Box
      component="section"
      style={{
        padding: 'var(--space-2xl) 40px',
        maxWidth: '1200px',
        margin: '0 auto',
      }}
    >
      <Box
        style={{
          background: 'linear-gradient(135deg, var(--color-burgundy-dark) 0%, var(--color-midnight) 100%)',
          color: 'var(--color-ivory)',
          padding: 'var(--space-2xl) var(--space-xl)',
          textAlign: 'center',
          maxWidth: '1000px',
          margin: '0 auto',
          position: 'relative',
          overflow: 'hidden',
          boxShadow: '0 20px 60px rgba(0,0,0,0.3)',
          borderRadius: '16px',
        }}
      >
        {/* Rotating background pattern */}
        <Box
          className="cta-rotate-animation"
          style={{
            position: 'absolute',
            top: '-50%',
            right: '-50%',
            width: '200%',
            height: '200%',
            backgroundImage: "url(\"data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E%3Ccircle cx='50' cy='50' r='30' fill='none' stroke='%23B76D75' stroke-width='0.5' opacity='0.2'/%3E%3C/svg%3E\")",
            backgroundSize: '100px 100px',
            pointerEvents: 'none',
          }}
        />

        <Container
          size="md"
          style={{
            position: 'relative',
            zIndex: 1,
          }}
        >
          <Text
            style={{
              fontFamily: 'var(--font-accent)',
              fontSize: '28px',
              color: 'var(--color-rose-gold)',
              marginBottom: 'var(--space-md)',
            }}
          >
            {subtitle}
          </Text>

          <Title
            order={2}
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '42px',
              fontWeight: 800,
              marginBottom: 'var(--space-md)',
              textTransform: 'uppercase',
              letterSpacing: '3px',
            }}
          >
            {title}
          </Title>

          <Text
            style={{
              fontSize: '20px',
              marginBottom: 'var(--space-xl)',
              opacity: 0.9,
              fontWeight: 300,
            }}
          >
            {description}
          </Text>

          {customButton || (
            <Button
              component={Link}
              to={buttonLink}
              size="lg"
              style={{
                padding: '18px 40px',
                fontSize: '16px',
                background: 'linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)',
                color: 'var(--color-midnight)',
                boxShadow: '0 6px 20px rgba(255, 191, 0, 0.4)',
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1.5px',
                transition: 'all 0.3s ease',
                '&:hover': {
                  boxShadow: '0 10px 30px rgba(255, 191, 0, 0.5)',
                  background: 'linear-gradient(135deg, var(--color-amber-dark) 0%, var(--color-amber) 100%)',
                  borderRadius: '6px 12px 6px 12px',
                }
              }}
            >
              {buttonText}
            </Button>
          )}
        </Container>
      </Box>
    </Box>
  );
};