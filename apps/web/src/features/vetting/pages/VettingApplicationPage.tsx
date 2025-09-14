// Main vetting application page
import React, { useState } from 'react';
import {
  Container,
  Box,
  Paper,
  Text,
  Button,
  Group,
  Alert,
  Stack,
  Title,
  Timeline,
  ThemeIcon,
  Anchor
} from '@mantine/core';
import {
  IconCheck,
  IconInfoCircle,
  IconShieldCheck,
  IconUsers,
  IconCalendar,
  IconMail
} from '@tabler/icons-react';
import { ApplicationForm } from '../components/application/ApplicationForm';
import { Link, useNavigate } from 'react-router-dom';

interface VettingApplicationPageProps {
  className?: string;
}

export const VettingApplicationPage: React.FC<VettingApplicationPageProps> = ({
  className
}) => {
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [submissionResult, setSubmissionResult] = useState<{
    applicationNumber: string;
    statusUrl: string;
  } | null>(null);
  const navigate = useNavigate();

  const handleSubmissionComplete = (applicationNumber: string, statusUrl: string) => {
    setSubmissionResult({ applicationNumber, statusUrl });
    setIsSubmitted(true);
    
    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  if (isSubmitted && submissionResult) {
    return (
      <Container size="md" py="xl" className={className}>
        <Paper p="xl" shadow="lg" ta="center">
          <Stack spacing="xl">
            <ThemeIcon size={80} color="green" variant="light" mx="auto">
              <IconCheck size={40} />
            </ThemeIcon>
            
            <Box>
              <Title order={1} c="green.7" mb="md">
                Application Submitted Successfully!
              </Title>
              <Text size="lg" c="dimmed">
                Thank you for applying to join WitchCityRope
              </Text>
            </Box>

            <Alert
              icon={<IconInfoCircle />}
              color="blue"
              title="What happens next?"
            >
              <Text size="sm" mb="md">
                Your application <strong>#{submissionResult.applicationNumber}</strong> has been received 
                and is now in our review queue.
              </Text>
              
              <Timeline active={1} color="blue" bulletSize={24} lineWidth={2}>
                <Timeline.Item bullet={<IconMail size={12} />} title="Email Confirmation">
                  <Text size="xs" c="dimmed">
                    You'll receive a confirmation email with your tracking information
                  </Text>
                </Timeline.Item>
                
                <Timeline.Item bullet={<IconUsers size={12} />} title="Reference Contact">
                  <Text size="xs" c="dimmed">
                    Your references will be contacted within 24-48 hours
                  </Text>
                </Timeline.Item>
                
                <Timeline.Item bullet={<IconCalendar size={12} />} title="Review Process">
                  <Text size="xs" c="dimmed">
                    Our vetting team will review your application within 5-7 business days
                  </Text>
                </Timeline.Item>
                
                <Timeline.Item bullet={<IconCheck size={12} />} title="Decision Notification">
                  <Text size="xs" c="dimmed">
                    You'll receive an email with our decision and next steps
                  </Text>
                </Timeline.Item>
              </Timeline>
            </Alert>

            <Group position="center" spacing="md">
              <Button
                component={Link}
                to="/vetting/status"
                color="blue"
                size="lg"
              >
                Check Application Status
              </Button>
              
              <Button
                variant="outline"
                color="wcr.7"
                size="lg"
                onClick={() => navigate('/')}
              >
                Return to Home
              </Button>
            </Group>

            <Paper p="md" bg="gray.0" withBorder>
              <Text size="sm" c="dimmed" ta="left">
                <strong>Important:</strong> Save your application number <strong>#{submissionResult.applicationNumber}</strong> 
                and check your email for the status tracking link. You can use either to check your application progress at any time.
              </Text>
            </Paper>
          </Stack>
        </Paper>
      </Container>
    );
  }

  return (
    <Container size="lg" py="xl" className={className}>
      {/* Page Header */}
      <Paper p="xl" shadow="sm" mb="xl" ta="center">
        <Stack spacing="lg">
          <Box>
            <Title order={1} size="h1" c="wcr.7" mb="md">
              Vetting Application
            </Title>
            <Text size="lg" c="dimmed" mx="auto" style={{ maxWidth: 600 }}>
              Welcome to the WitchCityRope community vetting process. This application helps us 
              ensure a safe and welcoming environment for all members.
            </Text>
          </Box>

          {/* Process Overview */}
          <Alert
            icon={<IconInfoCircle />}
            color="blue"
            title="Application Process Overview"
            styles={{ root: { textAlign: 'left' } }}
          >
            <Text size="sm" mb="md">
              Our vetting process consists of five simple steps:
            </Text>
            
            <Text size="sm" component="ol" pl="md">
              <li><strong>Personal Information</strong> - Basic contact details</li>
              <li><strong>Experience & Knowledge</strong> - Your rope bondage background</li>
              <li><strong>Community Understanding</strong> - Your goals and interests</li>
              <li><strong>References</strong> - Two community references (required)</li>
              <li><strong>Review & Submit</strong> - Final review and privacy options</li>
            </Text>
          </Alert>

          {/* Privacy Notice */}
          <Alert
            icon={<IconShieldCheck />}
            color="green"
            title="Privacy & Data Protection"
            styles={{ root: { textAlign: 'left' } }}
          >
            <Text size="sm">
              We take your privacy seriously. All personal information is encrypted and only 
              accessible to approved vetting team members. You can choose to apply anonymously, 
              and your data will never be shared outside the review process.
            </Text>
          </Alert>

          {/* Existing Members */}
          <Paper p="md" bg="grape.0" withBorder>
            <Text size="sm" weight={500} mb="xs">
              Already a member?
            </Text>
            <Text size="sm" c="dimmed" mb="md">
              If you're already a WitchCityRope member and need to update your information, 
              please use your member portal instead of submitting a new application.
            </Text>
            <Group position="center">
              <Anchor component={Link} to="/login" size="sm">
                Member Login
              </Anchor>
              <Anchor component={Link} to="/vetting/status" size="sm">
                Check Application Status
              </Anchor>
            </Group>
          </Paper>
        </Stack>
      </Paper>

      {/* Application Form */}
      <ApplicationForm onSubmissionComplete={handleSubmissionComplete} />

      {/* Footer Information */}
      <Paper p="lg" bg="gray.0" mt="xl">
        <Stack spacing="md">
          <Text size="sm" weight={500} c="wcr.7">
            Need Help?
          </Text>
          
          <Text size="sm" c="dimmed">
            If you have questions about the vetting process or need assistance with your application, 
            please don't hesitate to reach out to our team.
          </Text>
          
          <Group>
            <Button variant="light" size="sm" component={Link} to="/contact">
              Contact Support
            </Button>
            <Button variant="subtle" size="sm" component={Link} to="/vetting/faq">
              Vetting FAQ
            </Button>
          </Group>
        </Stack>
      </Paper>
    </Container>
  );
};