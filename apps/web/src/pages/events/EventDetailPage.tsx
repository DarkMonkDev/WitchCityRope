import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { 
  Container, Stack, Title, Text, Breadcrumbs, 
  Anchor, Alert, Button, Box, Badge, Group, Paper, 
  ActionIcon, List, Avatar
} from '@mantine/core';
import { 
  IconCalendar, IconClock, IconMapPin, IconUsers, 
  IconShare, IconMail, IconBrandX, IconLink, IconCheck
} from '@tabler/icons-react';
import { formatEventDate, formatEventTime } from '../../utils/eventUtils';

// Mock event data matching the wireframe
const mockEventDetail = {
  id: 'e2222222-2222-2222-2222-222222222222',
  title: '3-Day Rope Intensive Series',
  type: 'SERIES',
  dates: 'February 15-17, 2025',
  times: 'Various Times',
  location: 'Salem Studio',
  duration: '2.5 hours each day',
  maxCapacity: 20,
  description: `Immerse yourself in three days of comprehensive rope bondage education! This intensive series takes you from fundamental safety concepts through advanced techniques, with each day building upon the last. Perfect for those wanting to fast-track their learning or deepen existing knowledge.

Choose from full series passes for the complete experience, or attend individual days based on your needs and schedule. Our expert instructors ensure personalized attention in small group settings.`,
  dailyBreakdown: [
    {
      day: 1,
      date: 'Feb 15',
      title: 'Fundamentals',
      description: 'Safety protocols, basic ties, communication, and risk awareness. Perfect for beginners.'
    },
    {
      day: 2,
      date: 'Feb 16', 
      title: 'Intermediate Techniques',
      description: 'Chest harnesses, hip patterns, and transitional ties. Some experience recommended.'
    },
    {
      day: 3,
      date: 'Feb 17',
      title: 'Advanced Applications',
      description: 'Suspension prep, complex patterns, and advanced safety. Strong fundamentals required.'
    }
  ],
  prerequisites: [
    'Must be 21+ years of age',
    'Day 1: No experience required',
    'Day 2-3: Basic tie knowledge recommended'
  ],
  whatToBring: [
    '6-8 pieces of 8mm rope (available for purchase at venue)',
    'Comfortable clothing that allows movement', 
    'Water bottle and snacks',
    'Notebook for taking notes'
  ],
  instructor: {
    name: 'Alex Rivers',
    initials: 'AR',
    bio: 'Alex has been teaching rope bondage for over 8 years and is passionate about creating inclusive, safety-focused learning environments. Known for their patient teaching style and emphasis on consent culture, Alex makes learning accessible for all skill levels.'
  },
  sessions: [
    { name: 'Day 1 (Feb 15)', spotsLeft: 12, total: 20, status: 'high' },
    { name: 'Day 2 (Feb 16)', spotsLeft: 5, total: 20, status: 'low' },
    { name: 'Day 3 (Feb 17)', spotsLeft: 15, total: 20, status: 'high' }
  ],
  ticketOptions: [
    {
      id: 'full-series',
      name: 'Full 3-Day Series',
      price: '$165-225',
      sessions: ['Day 1', 'Day 2', 'Day 3'],
      spotsLeft: 5,
      status: 'low',
      available: true
    },
    {
      id: 'day-1',
      name: 'Day 1 Only',
      price: '$65-85',
      sessions: ['Day 1 (Fundamentals)'],
      spotsLeft: 12,
      status: 'high',
      available: true
    },
    {
      id: 'day-2',
      name: 'Day 2 Only',
      price: '$75-95',
      sessions: ['Day 2 (Intermediate)'],
      spotsLeft: 5,
      status: 'low',
      available: true
    },
    {
      id: 'day-3',
      name: 'Day 3 Only',
      price: '$85-105',
      sessions: ['Day 3 (Advanced)'],
      spotsLeft: 15,
      status: 'high',
      available: true
    },
    {
      id: 'days-1-2',
      name: 'Days 1-2 Combo',
      price: '$120-160',
      sessions: ['Day 1', 'Day 2'],
      spotsLeft: 0,
      status: 'sold-out',
      available: false
    }
  ]
};

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [selectedTicket, setSelectedTicket] = useState('full-series');
  const event = mockEventDetail; // In real app, fetch by ID

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'high': return 'var(--color-success)';
      case 'low': return 'var(--color-error)';
      default: return 'var(--color-warning)';
    }
  };

  const handlePurchase = () => {
    const selectedOption = event.ticketOptions.find(opt => opt.id === selectedTicket);
    console.log('Purchasing:', selectedOption?.name);
    // Implement purchase logic
  };

  return (
    <Box data-testid="page-event-detail" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
      {/* Breadcrumb */}
      <Container size="xl" pt="md">
        <Breadcrumbs separator="/" mb="md" styles={{
          breadcrumb: {
            color: 'var(--color-stone)',
            fontSize: '14px'
          }
        }}>
          <Anchor 
            href="/" 
            style={{ 
              color: 'var(--color-burgundy)',
              textDecoration: 'none',
              transition: 'color 0.3s ease'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.color = 'var(--color-burgundy-dark)';
              e.currentTarget.style.textDecoration = 'underline';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.color = 'var(--color-burgundy)';
              e.currentTarget.style.textDecoration = 'none';
            }}
          >
            Home
          </Anchor>
          <Anchor 
            href="/events"
            style={{ 
              color: 'var(--color-burgundy)',
              textDecoration: 'none',
              transition: 'color 0.3s ease'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.color = 'var(--color-burgundy-dark)';
              e.currentTarget.style.textDecoration = 'underline';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.color = 'var(--color-burgundy)';
              e.currentTarget.style.textDecoration = 'none';
            }}
          >
            Classes
          </Anchor>
          <Text style={{ color: 'var(--color-stone)' }}>3-Day Rope Intensive Series</Text>
        </Breadcrumbs>
      </Container>

      <Container size="xl" style={{ 
        display: 'grid', 
        gridTemplateColumns: '1fr 380px', 
        gap: 'var(--space-xl)',
        paddingBottom: 'var(--space-xl)'
      }}>
        {/* Left Column - Event Details */}
        <Stack gap="lg">
          {/* Event Hero Section */}
          <Paper
            data-testid="section-hero"
            style={{
              background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
              borderRadius: '24px',
              padding: 'var(--space-3xl)',
              position: 'relative',
              overflow: 'hidden'
            }}
          >
            {/* Subtle overlay */}
            <Box
              style={{
                position: 'absolute',
                top: '-50%',
                left: '-50%',
                width: '200%',
                height: '200%',
                background: 'radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%)',
                transform: 'rotate(45deg)'
              }}
            />
            
            <Box style={{ position: 'relative', zIndex: 1 }}>
              <Badge
                size="lg"
                style={{
                  background: 'linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)',
                  color: 'var(--color-midnight)',
                  padding: '6px 20px',
                  borderRadius: '20px',
                  fontSize: '14px',
                  fontWeight: 700,
                  textTransform: 'uppercase',
                  letterSpacing: '1px',
                  marginBottom: 'var(--space-md)',
                  boxShadow: '0 2px 10px rgba(255, 191, 0, 0.3)'
                }}
              >
                3-Day Series
              </Badge>

              <Title 
                order={1}
                style={{
                  fontFamily: 'var(--font-heading)',
                  fontSize: '48px',
                  fontWeight: 800,
                  color: 'var(--color-ivory)',
                  marginBottom: 'var(--space-md)',
                  lineHeight: 1.2
                }}
              >
                {event.title}
              </Title>

              <Group gap="lg" style={{ flexWrap: 'wrap' }}>
                <Group gap="xs" style={{ color: 'var(--color-dusty-rose)', fontSize: '20px' }}>
                  <IconCalendar size={20} />
                  <Text size="lg">{event.dates}</Text>
                </Group>
                <Group gap="xs" style={{ color: 'var(--color-dusty-rose)', fontSize: '20px' }}>
                  <IconClock size={20} />
                  <Text size="lg">{event.times}</Text>
                </Group>
                <Group gap="xs" style={{ color: 'var(--color-dusty-rose)', fontSize: '20px' }}>
                  <IconMapPin size={20} />
                  <Text size="lg">{event.location}</Text>
                </Group>
              </Group>
            </Box>
          </Paper>

          {/* About This Series */}
          <ContentSection title="About This Series">
            {event.description.split('\n\n').map((paragraph, index) => (
              <Text 
                key={index}
                style={{
                  fontSize: '17px',
                  lineHeight: 1.8,
                  color: 'var(--color-charcoal)',
                  marginBottom: 'var(--space-md)'
                }}
              >
                {paragraph}
              </Text>
            ))}

            <Title 
              order={2}
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '28px',
                fontWeight: 700,
                color: 'var(--color-burgundy)',
                marginTop: 'var(--space-lg)',
                marginBottom: 'var(--space-md)'
              }}
            >
              Daily Breakdown
            </Title>

            <List data-testid="section-sessions" spacing="md" style={{ listStyleType: 'none', padding: 0 }}>
              {event.dailyBreakdown.map((day) => (
                <List.Item key={day.day} style={{ marginBottom: 'var(--space-md)' }}>
                  <Group gap="sm" align="flex-start">
                    <Box
                      style={{
                        width: '24px',
                        height: '24px',
                        background: 'var(--color-burgundy)',
                        borderRadius: '50%',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        color: 'var(--color-ivory)',
                        flexShrink: 0,
                        marginTop: '2px',
                        fontSize: '14px',
                        fontWeight: 700
                      }}
                    >
                      {day.day}
                    </Box>
                    <Box>
                      <Text fw={700} mb={4}>
                        Day {day.day} - {day.title} ({day.date}):
                      </Text>
                      <Text style={{ color: 'var(--color-charcoal)' }}>
                        {day.description}
                      </Text>
                    </Box>
                  </Group>
                </List.Item>
              ))}
            </List>

            <Title 
              order={2}
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '28px',
                fontWeight: 700,
                color: 'var(--color-burgundy)',
                marginTop: 'var(--space-lg)',
                marginBottom: 'var(--space-md)'
              }}
            >
              Prerequisites & What to Bring
            </Title>

            <Title 
              order={4}
              style={{
                fontFamily: 'var(--font-heading)',
                marginBottom: 'var(--space-md)',
                marginTop: 'var(--space-md)'
              }}
            >
              Prerequisites
            </Title>
            
            {event.prerequisites.map((prereq, index) => (
              <Group key={index} gap="sm" mb="sm">
                <Text style={{ color: 'var(--color-success)' }}>✓</Text>
                <Text style={{ color: 'var(--color-smoke)' }}>{prereq}</Text>
              </Group>
            ))}

            <Title 
              order={4}
              style={{
                fontFamily: 'var(--font-heading)',
                marginBottom: 'var(--space-md)',
                marginTop: 'var(--space-md)'
              }}
            >
              What to Bring
            </Title>
            
            {event.whatToBring.map((item, index) => (
              <Group key={index} gap="sm" mb="sm">
                <Text>•</Text>
                <Text style={{ color: 'var(--color-smoke)' }}>{item}</Text>
              </Group>
            ))}
          </ContentSection>

          {/* About Your Instructor */}
          <ContentSection title="About Your Instructor">
            <Group gap="md" align="center" mt="lg" p="lg" style={{ 
              background: 'var(--color-cream)',
              borderRadius: '12px'
            }}>
              <Avatar
                size={80}
                style={{
                  background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
                  color: 'var(--color-ivory)',
                  fontSize: '32px',
                  fontWeight: 700,
                  boxShadow: '0 4px 15px rgba(136, 1, 36, 0.3)',
                  border: '2px solid var(--color-rose-gold)'
                }}
              >
                {event.instructor.initials}
              </Avatar>
              <Stack gap="xs" style={{ flex: 1 }}>
                <Title 
                  order={3}
                  style={{
                    fontFamily: 'var(--font-heading)',
                    fontSize: '20px',
                    fontWeight: 700,
                    color: 'var(--color-charcoal)'
                  }}
                >
                  {event.instructor.name}
                </Title>
                <Text style={{ color: 'var(--color-stone)', fontSize: '15px' }}>
                  {event.instructor.bio}
                </Text>
              </Stack>
            </Group>
          </ContentSection>

          {/* Important Policies */}
          <ContentSection title="Important Policies">
            <Text mb="md">
              <strong>Refund Policy:</strong> Full refund available up to 48 hours before the event. No refunds within 48 hours of start time.
            </Text>
            <Text mb="md">
              <strong>Age Requirement:</strong> All participants must be 21 or older.
            </Text>
            <Text>
              <strong>Code of Conduct:</strong> All attendees must follow our{' '}
              <Anchor href="#" style={{ color: 'var(--color-burgundy)', textDecoration: 'underline' }}>
                Code of Conduct
              </Anchor>.
            </Text>
          </ContentSection>
        </Stack>

        {/* Right Column - Registration Card */}
        <Box style={{ position: 'sticky', top: '100px', height: 'fit-content' }}>
          <RegistrationCard 
            event={event}
            selectedTicket={selectedTicket}
            onTicketSelect={setSelectedTicket}
            onPurchase={handlePurchase}
          />
        </Box>
      </Container>
    </Box>
  );
};

// Content Section Component
interface ContentSectionProps {
  title: string;
  children: React.ReactNode;
}

const ContentSection: React.FC<ContentSectionProps> = ({ title, children }) => (
  <Paper
    style={{
      background: 'var(--color-ivory)',
      borderRadius: '16px',
      padding: 'var(--space-xl)',
      boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
      border: '1px solid rgba(183, 109, 117, 0.1)',
      position: 'relative',
      overflow: 'hidden'
    }}
    onMouseEnter={(e) => {
      const shimmer = document.createElement('div');
      shimmer.style.position = 'absolute';
      shimmer.style.top = '-50%';
      shimmer.style.left = '-50%';
      shimmer.style.width = '200%';
      shimmer.style.height = '200%';
      shimmer.style.background = 'linear-gradient(45deg, transparent 30%, rgba(183, 109, 117, 0.05) 50%, transparent 70%)';
      shimmer.style.transform = 'rotate(45deg)';
      shimmer.style.animation = 'shimmer 0.5s ease';
      shimmer.style.pointerEvents = 'none';
      e.currentTarget.appendChild(shimmer);
      setTimeout(() => shimmer.remove(), 500);
    }}
  >
    <Title 
      order={2}
      style={{
        fontFamily: 'var(--font-heading)',
        fontSize: '28px',
        fontWeight: 700,
        color: 'var(--color-burgundy)',
        marginBottom: 'var(--space-md)'
      }}
    >
      {title}
    </Title>
    {children}
  </Paper>
);

// Registration Card Component
interface RegistrationCardProps {
  event: typeof mockEventDetail;
  selectedTicket: string;
  onTicketSelect: (ticketId: string) => void;
  onPurchase: () => void;
}

const RegistrationCard: React.FC<RegistrationCardProps> = ({ 
  event, 
  selectedTicket, 
  onTicketSelect, 
  onPurchase 
}) => {
  const constrainedSession = event.sessions.find(s => s.status === 'low');
  const selectedOption = event.ticketOptions.find(opt => opt.id === selectedTicket);

  return (
    <Paper
      style={{
        background: 'var(--color-ivory)',
        borderRadius: '16px',
        padding: 'var(--space-lg)',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
        border: '1px solid rgba(183, 109, 117, 0.1)',
        transition: 'all 0.3s ease'
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.boxShadow = '0 8px 24px rgba(0,0,0,0.08)';
        e.currentTarget.style.borderColor = 'rgba(183, 109, 117, 0.2)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.05)';
        e.currentTarget.style.borderColor = 'rgba(183, 109, 117, 0.1)';
      }}
    >
      <Stack gap="lg">
        {/* Capacity Warning */}
        {constrainedSession && (
          <Alert
            style={{
              background: 'linear-gradient(135deg, rgba(220, 20, 60, 0.1), rgba(218, 165, 32, 0.1))',
              border: '2px solid var(--color-warning)',
              borderRadius: '8px',
              padding: 'var(--space-sm)',
              textAlign: 'center'
            }}
          >
            <Group gap="xs" justify="center">
              <Text style={{ color: 'var(--color-warning)', fontSize: '18px' }}>⚠️</Text>
              <Text style={{ fontSize: '14px', color: 'var(--color-charcoal)', fontWeight: 600 }}>
                {constrainedSession.name} filling up fast - only {constrainedSession.spotsLeft} spots left!
              </Text>
            </Group>
          </Alert>
        )}

        {/* Session Availability */}
        <Box
          style={{
            background: 'var(--color-cream)',
            borderRadius: '12px',
            padding: 'var(--space-md)'
          }}
        >
          <Title 
            order={4}
            ta="center"
            mb="md"
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '16px',
              fontWeight: 600,
              color: 'var(--color-charcoal)'
            }}
          >
            Session Availability
          </Title>
          {event.sessions.map((session) => (
            <Group 
              key={session.name}
              justify="space-between" 
              align="center"
              py="xs"
              style={{ borderBottom: '1px solid rgba(183, 109, 117, 0.1)' }}
            >
              <Text style={{ fontSize: '14px', color: 'var(--color-charcoal)' }}>
                {session.name}
              </Text>
              <Text 
                style={{ 
                  fontSize: '14px', 
                  fontWeight: 600,
                  color: getStatusColor(session.status)
                }}
              >
                {session.spotsLeft} spots left
              </Text>
            </Group>
          ))}
        </Box>

        {/* Ticket Selection */}
        <Stack gap="sm">
          <Title
            order={4}
            ta="center"
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '18px',
              fontWeight: 600,
              color: 'var(--color-charcoal)'
            }}
          >
            Choose Your Ticket
          </Title>

          {event.ticketOptions.map((option) => (
            <Box
              key={option.id}
              onClick={() => option.available && onTicketSelect(option.id)}
              style={{
                background: option.available ? 'var(--color-cream)' : 'var(--color-stone)',
                border: `2px solid ${selectedTicket === option.id ? 'var(--color-burgundy)' : 'var(--color-taupe)'}`,
                borderRadius: '12px',
                padding: 'var(--space-md)',
                cursor: option.available ? 'pointer' : 'not-allowed',
                transition: 'all 0.3s ease',
                opacity: option.available ? 1 : 0.6,
                ...(selectedTicket === option.id && option.available && {
                  background: 'rgba(136, 1, 36, 0.05)'
                })
              }}
              onMouseEnter={(e) => {
                if (option.available && selectedTicket !== option.id) {
                  e.currentTarget.style.borderColor = 'var(--color-rose-gold)';
                  e.currentTarget.style.transform = 'translateY(-2px)';
                  e.currentTarget.style.boxShadow = '0 4px 12px rgba(183, 109, 117, 0.15)';
                }
              }}
              onMouseLeave={(e) => {
                if (option.available && selectedTicket !== option.id) {
                  e.currentTarget.style.borderColor = 'var(--color-taupe)';
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = 'none';
                }
              }}
            >
              <Group justify="space-between" align="center" mb="xs">
                <Text 
                  fw={600}
                  style={{
                    fontFamily: 'var(--font-heading)',
                    color: option.available ? 'var(--color-charcoal)' : 'var(--color-ivory)',
                    fontSize: '16px'
                  }}
                >
                  {option.name}
                </Text>
                <Text 
                  fw={700}
                  style={{
                    fontFamily: 'var(--font-heading)',
                    color: option.available ? 'var(--color-burgundy)' : 'var(--color-ivory)',
                    fontSize: '18px'
                  }}
                >
                  {option.price}
                </Text>
              </Group>

              <Text 
                size="sm"
                mb="xs"
                style={{ 
                  color: option.available ? 'var(--color-stone)' : 'var(--color-ivory)',
                  fontSize: '14px'
                }}
              >
                {option.sessions.map((session, idx) => (
                  <span key={idx}>
                    <span style={{ color: 'var(--color-success)', fontWeight: 700 }}>✓</span> {session}
                    {idx < option.sessions.length - 1 && ' '}
                  </span>
                ))}
              </Text>

              <Text 
                size="sm"
                fw={600}
                style={{
                  fontSize: '13px',
                  color: option.available 
                    ? getStatusColor(option.status)
                    : 'var(--color-ivory)'
                }}
              >
                {option.available 
                  ? `${option.spotsLeft} ${option.id.includes('series') ? 'series passes' : 'spots'} left`
                  : 'Sold Out (Day 2 full)'
                }
              </Text>
            </Box>
          ))}
        </Stack>

        {/* Purchase Button */}
        <Button
          data-testid="button-register"
          onClick={onPurchase}
          disabled={!selectedOption?.available}
          style={{
            width: '100%',
            padding: 'var(--space-md)',
            background: selectedOption?.available 
              ? 'linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)'
              : 'var(--color-stone)',
            color: selectedOption?.available ? 'var(--color-midnight)' : 'var(--color-ivory)',
            border: 'none',
            borderRadius: '8px',
            fontFamily: 'var(--font-heading)',
            fontSize: '18px',
            fontWeight: 700,
            textTransform: 'uppercase',
            letterSpacing: '1px',
            cursor: selectedOption?.available ? 'pointer' : 'not-allowed',
            transition: 'all 0.3s ease',
            boxShadow: selectedOption?.available ? '0 4px 15px rgba(255, 191, 0, 0.4)' : 'none',
            position: 'relative',
            overflow: 'hidden'
          }}
          onMouseEnter={(e) => {
            if (selectedOption?.available) {
              e.currentTarget.style.transform = 'translateY(-2px)';
              e.currentTarget.style.boxShadow = '0 6px 25px rgba(255, 191, 0, 0.5)';
              e.currentTarget.style.background = 'linear-gradient(135deg, var(--color-amber-dark) 0%, var(--color-amber) 100%)';
            }
          }}
          onMouseLeave={(e) => {
            if (selectedOption?.available) {
              e.currentTarget.style.transform = 'translateY(0)';
              e.currentTarget.style.boxShadow = '0 4px 15px rgba(255, 191, 0, 0.4)';
              e.currentTarget.style.background = 'linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)';
            }
          }}
        >
          {selectedOption 
            ? `Purchase ${selectedOption.name}`
            : 'Purchase Tickets'
          }
        </Button>

        {/* Event Details */}
        <Stack gap={4}>
          <Group justify="space-between" style={{ padding: '4px 0', borderBottom: '1px solid var(--color-cream)' }}>
            <Text style={{ color: 'var(--color-stone)', fontSize: '15px' }}>Location</Text>
            <Text fw={600} style={{ color: 'var(--color-charcoal)' }}>{event.location}</Text>
          </Group>
          <Group justify="space-between" style={{ padding: '4px 0', borderBottom: '1px solid var(--color-cream)' }}>
            <Text style={{ color: 'var(--color-stone)', fontSize: '15px' }}>Duration</Text>
            <Text fw={600} style={{ color: 'var(--color-charcoal)' }}>{event.duration}</Text>
          </Group>
          <Group justify="space-between" style={{ padding: '4px 0' }}>
            <Text style={{ color: 'var(--color-stone)', fontSize: '15px' }}>Class Size</Text>
            <Text fw={600} style={{ color: 'var(--color-charcoal)' }}>Max {event.maxCapacity} per session</Text>
          </Group>
        </Stack>

        {/* Share Section */}
        <Box pt="lg" style={{ borderTop: '1px solid var(--color-taupe)', textAlign: 'center' }}>
          <Text mb="sm" style={{ fontSize: '14px', color: 'var(--color-stone)' }}>
            Share this event
          </Text>
          <Group gap="sm" justify="center">
            {[
              { icon: IconMail, label: 'Email' },
              { icon: IconBrandX, label: 'Social' },
              { icon: IconLink, label: 'Copy Link' }
            ].map(({ icon: Icon, label }) => (
              <ActionIcon
                key={label}
                size={40}
                style={{
                  borderRadius: '50%',
                  background: 'var(--color-cream)',
                  border: '2px solid var(--color-taupe)',
                  color: 'var(--color-burgundy)',
                  transition: 'all 0.3s ease',
                  position: 'relative',
                  overflow: 'hidden'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.color = 'var(--color-ivory)';
                  e.currentTarget.style.borderColor = 'var(--color-burgundy)';
                  e.currentTarget.style.transform = 'scale(1.1)';
                  e.currentTarget.style.background = 'var(--color-burgundy)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.color = 'var(--color-burgundy)';
                  e.currentTarget.style.borderColor = 'var(--color-taupe)';
                  e.currentTarget.style.transform = 'scale(1)';
                  e.currentTarget.style.background = 'var(--color-cream)';
                }}
              >
                <Icon size={16} />
              </ActionIcon>
            ))}
          </Group>
        </Box>
      </Stack>
    </Paper>
  );
};

// Helper function for status colors
const getStatusColor = (status: string) => {
  switch (status) {
    case 'high': return 'var(--color-success)';
    case 'low': return 'var(--color-error)';
    case 'sold-out': return 'var(--color-stone)';
    default: return 'var(--color-warning)';
  }
};