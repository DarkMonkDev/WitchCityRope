import React, { useState, useEffect } from 'react';
import { Accordion, Box, Group, Anchor, Text } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { Link } from 'react-router-dom';
import { IconChevronDown } from '@tabler/icons-react';
import './Footer.css';

/**
 * Footer Component - Accordion Mobile-First Design
 *
 * Implements approved footer design with:
 * - Mobile (<768px): Collapsible accordion sections
 * - Desktop (≥768px): Three-column grid, all sections expanded
 * - Rose gold accents, midnight background
 * - Accessibility-first keyboard navigation
 * - WCAG 2.1 AA compliant
 *
 * Design Spec: /docs/functional-areas/footer-design/new-work/2025-11-08-footer-research/design/footer-design-specification.md
 */
export const Footer: React.FC = () => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const [expandedSections, setExpandedSections] = useState<string[]>([]);

  // Initialize accordion state based on viewport
  useEffect(() => {
    if (isMobile) {
      setExpandedSections([]); // Collapse all on mobile
    } else {
      setExpandedSections(['about', 'legal', 'connect']); // Expand all on desktop
    }
  }, [isMobile]);

  // FetLife SVG icon (checkmark style from mockup)
  const FetLifeIcon = () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <polyline points="20 6 9 17 4 12" />
    </svg>
  );

  // Instagram SVG icon (camera style from mockup)
  const InstagramIcon = () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <rect x="2" y="2" width="20" height="20" rx="5" ry="5" />
      <path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z" />
      <line x1="17.5" y1="6.5" x2="17.51" y2="6.5" />
    </svg>
  );

  return (
    <Box component="footer" className="footer">
      <Box className="footer-content">
        {isMobile ? (
          // Mobile: Accordion layout
          <Accordion
            value={expandedSections}
            onChange={setExpandedSections}
            multiple
            chevron={<IconChevronDown size={20} color="var(--color-rose-gold)" />}
            styles={{
              control: {
                backgroundColor: 'transparent',
                border: 'none',
                padding: 'var(--space-sm) 0',
                '&:hover': {
                  backgroundColor: 'transparent',
                },
              },
              item: {
                borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
                borderRadius: 0,
                backgroundColor: 'transparent',
                '&:last-of-type': {
                  borderBottom: 'none',
                },
              },
              panel: {
                paddingTop: 0,
                paddingBottom: 'var(--space-md)',
              },
              chevron: {
                color: 'var(--color-rose-gold)',
              },
            }}
          >
            {/* About Section */}
            <Accordion.Item value="about" className="footer-section">
              <Accordion.Control>
                <h3 className="footer-section-title">About</h3>
              </Accordion.Control>
              <Accordion.Panel>
                <ul className="footer-links">
                  <li><Link to="/about-us">About Us</Link></li>
                  <li><Link to="/code-of-conduct">Code of Conduct</Link></li>
                  <li><Link to="/faq">FAQ</Link></li>
                </ul>
              </Accordion.Panel>
            </Accordion.Item>

            {/* Legal Section */}
            <Accordion.Item value="legal" className="footer-section">
              <Accordion.Control>
                <h3 className="footer-section-title">Legal</h3>
              </Accordion.Control>
              <Accordion.Panel>
                <ul className="footer-links">
                  <li><Link to="/privacy-policy">Privacy Policy</Link></li>
                  <li><Link to="/terms-of-service">Terms of Service</Link></li>
                  <li><Link to="/event-waiver">Event Waiver</Link></li>
                </ul>
              </Accordion.Panel>
            </Accordion.Item>

            {/* Connect Section */}
            <Accordion.Item value="connect" className="footer-section">
              <Accordion.Control>
                <h3 className="footer-section-title">Connect</h3>
              </Accordion.Control>
              <Accordion.Panel>
                <ul className="footer-links">
                  <li><Link to="/contact-us">Contact Us</Link></li>
                </ul>
                <Group gap="md" mt="sm" className="social-links">
                  <Anchor
                    href="https://fetlife.com/witchcityrope"
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label="Follow us on FetLife"
                    className="social-link"
                  >
                    <FetLifeIcon />
                    <span>FetLife</span>
                  </Anchor>
                  <Anchor
                    href="https://instagram.com/witchcityrope"
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label="Follow us on Instagram"
                    className="social-link"
                  >
                    <InstagramIcon />
                    <span>Instagram</span>
                  </Anchor>
                </Group>
              </Accordion.Panel>
            </Accordion.Item>
          </Accordion>
        ) : (
          // Desktop: Grid layout
          <Box className="footer-desktop-grid">
            {/* About Section */}
            <div className="footer-section">
              <h3 className="footer-section-title">About</h3>
              <ul className="footer-links">
                <li><Link to="/about-us">About Us</Link></li>
                <li><Link to="/code-of-conduct">Code of Conduct</Link></li>
                <li><Link to="/faq">FAQ</Link></li>
              </ul>
            </div>

            {/* Legal Section */}
            <div className="footer-section">
              <h3 className="footer-section-title">Legal</h3>
              <ul className="footer-links">
                <li><Link to="/privacy-policy">Privacy Policy</Link></li>
                <li><Link to="/terms-of-service">Terms of Service</Link></li>
                <li><Link to="/event-waiver">Event Waiver</Link></li>
              </ul>
            </div>

            {/* Connect Section */}
            <div className="footer-section">
              <h3 className="footer-section-title">Connect</h3>
              <ul className="footer-links">
                <li><Link to="/contact-us">Contact Us</Link></li>
              </ul>
              <Group gap="md" mt="sm" className="social-links">
                <Anchor
                  href="https://fetlife.com/witchcityrope"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="Follow us on FetLife"
                  className="social-link"
                >
                  <FetLifeIcon />
                  <span>FetLife</span>
                </Anchor>
                <Anchor
                  href="https://instagram.com/witchcityrope"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="Follow us on Instagram"
                  className="social-link"
                >
                  <InstagramIcon />
                  <span>Instagram</span>
                </Anchor>
              </Group>
            </div>
          </Box>
        )}

        {/* Footer Bottom Section */}
        <div className="footer-bottom">
          <Text className="privacy-notice" size="xs">
            We protect the privacy of our members. Location details are shared only with vetted members and ticket holders.
          </Text>
          <Group className="footer-meta" justify={isMobile ? 'flex-start' : 'space-between'} gap={isMobile ? 'xs' : 'md'}>
            <Text size="xs">&copy; 2025 WitchCityRope. All rights reserved.</Text>
            <Anchor href="mailto:info@witchcityrope.com" size="xs" className="footer-email">
              info@witchcityrope.com
            </Anchor>
          </Group>
        </div>
      </Box>
    </Box>
  );
};
