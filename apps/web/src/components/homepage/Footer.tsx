import React from 'react';
import { Box, Text, Anchor } from '@mantine/core';

export const Footer: React.FC = () => {
  return (
    <Box
      component="footer"
      style={{
        background: 'var(--color-midnight)',
        color: 'var(--color-taupe)',
        padding: 'var(--space-2xl) 40px var(--space-xl)',
        marginTop: 'var(--space-2xl)',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      {/* Top gradient line */}
      <Box
        style={{
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          height: '1px',
          background: 'linear-gradient(90deg, transparent, var(--color-rose-gold), transparent)',
        }}
      />

      {/* Footer Content Grid */}
      <Box
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
          gap: 'var(--space-xl)',
          marginBottom: 'var(--space-xl)',
          maxWidth: '1200px',
          marginLeft: 'auto',
          marginRight: 'auto',
        }}
      >
        {/* Education Section */}
        <Box>
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              marginBottom: 'var(--space-md)',
              color: 'var(--color-rose-gold)',
              fontSize: '16px',
              textTransform: 'uppercase',
              letterSpacing: '2px',
              fontWeight: 700,
            }}
          >
            Education
          </Text>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Class Calendar
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Private Instruction
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Online Resources
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Safety Protocols
          </Anchor>
        </Box>

        {/* Community Section */}
        <Box>
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              marginBottom: 'var(--space-md)',
              color: 'var(--color-rose-gold)',
              fontSize: '16px',
              textTransform: 'uppercase',
              letterSpacing: '2px',
              fontWeight: 700,
            }}
          >
            Community
          </Text>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Membership
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Code of Conduct
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Our Instructors
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Testimonials
          </Anchor>
        </Box>

        {/* Resources Section */}
        <Box>
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              marginBottom: 'var(--space-md)',
              color: 'var(--color-rose-gold)',
              fontSize: '16px',
              textTransform: 'uppercase',
              letterSpacing: '2px',
              fontWeight: 700,
            }}
          >
            Resources
          </Text>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Equipment Guide
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Venue Information
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            FAQ
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Blog
          </Anchor>
        </Box>

        {/* Connect Section */}
        <Box>
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              marginBottom: 'var(--space-md)',
              color: 'var(--color-rose-gold)',
              fontSize: '16px',
              textTransform: 'uppercase',
              letterSpacing: '2px',
              fontWeight: 700,
            }}
          >
            Connect
          </Text>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Discord Community
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Newsletter
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            FetLife Group
          </Anchor>
          <Anchor
            href="#"
            className="footer-link"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              display: 'block',
              padding: 'var(--space-xs) 0',
              transition: 'color 0.3s ease',
            }}
          >
            Instagram
          </Anchor>
        </Box>
      </Box>

      {/* Footer Bottom */}
      <Box
        style={{
          textAlign: 'center',
          paddingTop: 'var(--space-lg)',
          borderTop: '1px solid rgba(183, 109, 117, 0.2)',
          color: 'var(--color-stone)',
          maxWidth: '1200px',
          margin: '0 auto',
        }}
      >
        <Text style={{ fontSize: '14px' }}>
          &copy; 2024 Witch City Rope. All rights reserved. |{' '}
          <Anchor
            href="#"
            style={{
              color: 'var(--color-rose-gold)',
              textDecoration: 'none',
              transition: 'color 0.3s ease',
            }}
          >
            Privacy Policy
          </Anchor>{' '}
          |{' '}
          <Anchor
            href="#"
            style={{
              color: 'var(--color-rose-gold)',
              textDecoration: 'none',
              transition: 'color 0.3s ease',
            }}
          >
            Terms of Use
          </Anchor>{' '}
          |{' '}
          <Anchor
            href="#"
            style={{
              color: 'var(--color-rose-gold)',
              textDecoration: 'none',
              transition: 'color 0.3s ease',
            }}
          >
            Age 21+ Community
          </Anchor>
        </Text>
      </Box>
    </Box>
  );
};