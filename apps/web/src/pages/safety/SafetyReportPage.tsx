// Safety Report Page
// Public page for submitting safety incident reports

import React from 'react';
import { Container, Box } from '@mantine/core';
import { IncidentReportForm } from '../../features/safety/components/IncidentReportForm';
import { useNavigate } from 'react-router-dom';

export function SafetyReportPage() {
  const navigate = useNavigate();
  
  const handleSubmissionComplete = (referenceNumber: string) => {
    // Navigate to status tracking page with reference number
    navigate(`/safety/status?ref=${referenceNumber}`);
  };
  
  return (
    <Container size="lg" py="xl">
      <Box>
        <IncidentReportForm onSubmissionComplete={handleSubmissionComplete} />
      </Box>
    </Container>
  );
}