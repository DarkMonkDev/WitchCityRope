import React from 'react';
import { Box } from '@mantine/core';
import { 
  HeroSection, 
  EventsList, 
  FeatureGrid, 
  CTASection, 
  RopeDivider 
} from '../components/homepage';
import { useUser, useIsAuthenticated } from '../stores/authStore';

export const HomePage: React.FC = () => {
  const user = useUser();
  const isAuthenticated = useIsAuthenticated();

  return (
    <Box>
      {/* Hero Section */}
      <HeroSection />

      {/* Classes & Events Section */}
      <EventsList />

      {/* Decorative Rope Divider */}
      <RopeDivider />

      {/* Features Section */}
      <FeatureGrid />

      {/* Call to Action Section */}
      <CTASection />
    </Box>
  );
};
