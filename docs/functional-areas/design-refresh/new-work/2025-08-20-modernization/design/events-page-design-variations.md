# Events Page Design Variations
<!-- Last Updated: 2025-08-20 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete - Ready for Stakeholder Review -->

## Design Overview

This document presents 5 comprehensive events page design variations that align with the corresponding homepage design variations, showcasing community events, workshops, and performances while maintaining visual consistency and optimizing for event discovery, registration, and community engagement.

**Event Page Features**: Grid/list views, filtering, search, calendar view, RSVP functionality, mobile-first responsive design, and dark/light theme support using Mantine v7 components.

## Event Types Displayed

### Community Event Categories
- **Workshops**: Skill-building sessions (beginner to advanced levels)
- **Performances**: Demonstrations, shows, artistic presentations
- **Social Events**: Munches, parties, community gatherings
- **Educational Sessions**: Safety courses, technique workshops
- **Special Events**: Competitions, fundraisers, celebrations

### Event Information Architecture
Each event card/item displays:
- Event title and type badge
- Date, time, and duration
- Location (venue or online)
- Instructor/host information and bio
- Member level requirements (vetted/non-vetted)
- Available spots with capacity indicator
- Price/donation information
- Quick RSVP/registration button
- Event description and prerequisites

---

## Variation 1: Enhanced Current Events (Subtle Evolution)
**Edginess Level**: 2/5 | **Alignment**: Homepage Variation 1

### Design Philosophy
Clean, polished events page that enhances the current successful design with improved micro-interactions, refined visual hierarchy, and seamless event discovery patterns.

### Visual Characteristics
- **Color Palette**: Original burgundy (#880124) with enhanced rose gold (#B76D75)
- **Card Design**: Clean white cards with subtle shadows and hover elevation
- **Typography**: Clear hierarchy with gradient accents on event titles
- **Interactions**: Smooth hover states with gentle animation effects

### Layout Structure
```
+--------------------------------------------------+
|                Header Navigation                 |
+--------------------------------------------------+
| Events                                [🔍] [⚙]  |
| Home / Events                                    |
+--------------------------------------------------+
| ┌─────────────────┬─────────────────────────────┐|
| │ Filters Sidebar │ Event Grid/List             ││
| │ ┌─────────────┐ │ ┌─────┐ ┌─────┐ ┌─────┐    ││
| │ │Search       │ │ │Event│ │Event│ │Event│    ││
| │ └─────────────┘ │ │ 1   │ │ 2   │ │ 3   │    ││
| │ Event Type      │ └─────┘ └─────┘ └─────┘    ││
| │ □ Workshops     │ ┌─────┐ ┌─────┐ ┌─────┐    ││
| │ □ Performances  │ │Event│ │Event│ │Event│    ││
| │ □ Social        │ │ 4   │ │ 5   │ │ 6   │    ││
| │ □ Educational   │ └─────┘ └─────┘ └─────┘    ││
| │ Date Range      │                             ││
| │ □ This Week     │ [Load More Events]          ││
| │ □ This Month    │                             ││
| │ Member Level    │                             ││
| │ □ All Members   │                             ││
| │ □ Vetted Only   │                             ││
| └─────────────────┴─────────────────────────────┘|
+--------------------------------------------------+
```

### Mantine v7 Implementation
```typescript
import { 
  Grid, 
  Card, 
  Image, 
  Text, 
  Badge, 
  Button, 
  Group, 
  Stack, 
  Container,
  Title,
  TextInput,
  Select,
  Checkbox,
  ActionIcon,
  Tooltip,
  Avatar,
  Progress,
  Anchor
} from '@mantine/core';
import { 
  IconSearch, 
  IconFilter, 
  IconCalendar, 
  IconMapPin, 
  IconUsers, 
  IconHeart, 
  IconShare 
} from '@tabler/icons-react';

const EnhancedCurrentEvents = () => {
  const [viewMode, setViewMode] = useState('grid');
  const [filters, setFilters] = useState({
    eventType: '',
    dateRange: '',
    memberLevel: 'all',
    search: ''
  });

  return (
    <Container size="xl" px="md" py="lg">
      {/* Page Header */}
      <Group justify="space-between" mb="xl">
        <div>
          <Title order={1} size="2.5rem" fw="bold" mb="xs">
            Community Events
          </Title>
          <Text size="lg" c="dimmed">
            Discover workshops, performances, and gatherings in Salem's rope community
          </Text>
        </div>
        
        <Group gap="sm">
          <TextInput
            placeholder="Search events..."
            leftSection={<IconSearch size={16} />}
            value={filters.search}
            onChange={(e) => setFilters({...filters, search: e.target.value})}
            styles={{
              input: {
                '&:focus': {
                  borderColor: 'var(--mantine-color-witchcity-6)',
                  boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)'
                }
              }
            }}
          />
          <ActionIcon variant="light" color="witchcity" size="lg">
            <IconFilter size={18} />
          </ActionIcon>
        </Group>
      </Group>

      <Grid>
        {/* Filters Sidebar */}
        <Grid.Col span={{ base: 12, md: 3 }}>
          <Card shadow="sm" radius="md" p="lg" withBorder>
            <Stack gap="lg">
              <div>
                <Text fw={600} mb="sm">Event Type</Text>
                <Stack gap="xs">
                  {['Workshops', 'Performances', 'Social', 'Educational'].map(type => (
                    <Checkbox 
                      key={type}
                      label={type}
                      color="witchcity"
                      styles={{
                        label: { fontSize: '0.9rem' }
                      }}
                    />
                  ))}
                </Stack>
              </div>

              <div>
                <Text fw={600} mb="sm">Date Range</Text>
                <Select
                  placeholder="Select timeframe"
                  data={[
                    { value: 'week', label: 'This Week' },
                    { value: 'month', label: 'This Month' },
                    { value: 'quarter', label: 'Next 3 Months' },
                    { value: 'all', label: 'All Upcoming' }
                  ]}
                  styles={{
                    input: {
                      '&:focus': {
                        borderColor: 'var(--mantine-color-witchcity-6)'
                      }
                    }
                  }}
                />
              </div>

              <div>
                <Text fw={600} mb="sm">Member Level</Text>
                <Stack gap="xs">
                  <Checkbox label="All Members" color="witchcity" defaultChecked />
                  <Checkbox label="Vetted Members Only" color="witchcity" />
                </Stack>
              </div>

              <div>
                <Text fw={600} mb="sm">Instructor</Text>
                <Select
                  placeholder="Filter by instructor"
                  data={[
                    { value: 'sarah', label: 'Sarah Mitchell' },
                    { value: 'alex', label: 'Alex Chen' },
                    { value: 'jamie', label: 'Jamie Rodriguez' }
                  ]}
                  searchable
                />
              </div>
            </Stack>
          </Card>
        </Grid.Col>

        {/* Events Grid */}
        <Grid.Col span={{ base: 12, md: 9 }}>
          <Group justify="space-between" mb="lg">
            <Text c="dimmed">Showing 24 of 156 events</Text>
            <Group gap="xs">
              <ActionIcon 
                variant={viewMode === 'grid' ? 'filled' : 'light'} 
                color="witchcity"
                onClick={() => setViewMode('grid')}
              >
                ⊞
              </ActionIcon>
              <ActionIcon 
                variant={viewMode === 'list' ? 'filled' : 'light'} 
                color="witchcity"
                onClick={() => setViewMode('list')}
              >
                ☰
              </ActionIcon>
            </Group>
          </Group>

          {/* Event Cards Grid */}
          <Grid>
            {Array.from({length: 6}).map((_, index) => (
              <Grid.Col key={index} span={{ base: 12, sm: 6, lg: 4 }}>
                <Card
                  shadow="sm"
                  radius="lg"
                  withBorder
                  style={{
                    transition: 'all 0.3s ease',
                    cursor: 'pointer'
                  }}
                  styles={{
                    root: {
                      '&:hover': {
                        transform: 'translateY(-4px)',
                        boxShadow: '0 10px 30px rgba(136, 1, 36, 0.15)'
                      }
                    }
                  }}
                >
                  {/* Event Image */}
                  <Card.Section>
                    <Image
                      src={`https://picsum.photos/400/200?random=${index}`}
                      height={200}
                      alt="Event image"
                    />
                    <Badge
                      variant="filled"
                      color="witchcity"
                      style={{
                        position: 'absolute',
                        top: 12,
                        left: 12
                      }}
                    >
                      Workshop
                    </Badge>
                  </Card.Section>

                  {/* Event Content */}
                  <Stack gap="sm" p="md">
                    <Group justify="space-between" align="flex-start">
                      <div style={{ flex: 1 }}>
                        <Title order={4} lineClamp={2} mb="xs">
                          Advanced Suspension Techniques
                        </Title>
                        <Text size="sm" c="dimmed" lineClamp={2}>
                          Master advanced suspension safety and artistic positions with expert guidance.
                        </Text>
                      </div>
                      <ActionIcon variant="subtle" color="red">
                        <IconHeart size={18} />
                      </ActionIcon>
                    </Group>

                    {/* Event Details */}
                    <Stack gap="xs">
                      <Group gap="xs">
                        <IconCalendar size={16} color="var(--mantine-color-dimmed)" />
                        <Text size="sm" c="dimmed">Sat, Dec 14 • 2:00 PM - 5:00 PM</Text>
                      </Group>
                      
                      <Group gap="xs">
                        <IconMapPin size={16} color="var(--mantine-color-dimmed)" />
                        <Text size="sm" c="dimmed">Salem Arts Center</Text>
                      </Group>

                      <Group gap="xs">
                        <Avatar size="sm" radius="xl">SM</Avatar>
                        <Text size="sm" c="dimmed">Sarah Mitchell</Text>
                      </Group>

                      <Group gap="xs">
                        <IconUsers size={16} color="var(--mantine-color-dimmed)" />
                        <Text size="sm" c="dimmed">8 of 12 spots filled</Text>
                        <Progress 
                          value={67} 
                          size="xs" 
                          color="witchcity" 
                          style={{ flex: 1 }}
                        />
                      </Group>
                    </Stack>

                    {/* Price and Action */}
                    <Group justify="space-between" align="center" mt="sm">
                      <div>
                        <Text fw={600} size="lg" c="witchcity">$85</Text>
                        <Text size="xs" c="dimmed">Sliding scale available</Text>
                      </div>
                      <Button 
                        variant="gradient"
                        gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}
                        size="sm"
                        styles={{
                          root: {
                            '&:hover': {
                              transform: 'translateY(-1px)',
                              boxShadow: '0 4px 15px rgba(136, 1, 36, 0.3)'
                            }
                          }
                        }}
                      >
                        Register
                      </Button>
                    </Group>

                    {/* Member Level Badge */}
                    <Badge 
                      variant="light" 
                      color="blue" 
                      size="xs"
                      style={{ alignSelf: 'flex-start' }}
                    >
                      Vetted Members Only
                    </Badge>
                  </Stack>
                </Card>
              </Grid.Col>
            ))}
          </Grid>

          {/* Load More */}
          <Group justify="center" mt="xl">
            <Button variant="outline" color="witchcity" size="lg">
              Load More Events
            </Button>
          </Group>
        </Grid.Col>
      </Grid>
    </Container>
  );
};
```

### Key Features
- **Clean Event Cards**: Professional design with subtle hover animations
- **Comprehensive Filtering**: Event type, date, member level, instructor filters
- **Grid/List Toggle**: Flexible viewing options
- **Capacity Indicators**: Visual progress bars showing event capacity
- **Member Level Badges**: Clear indication of access requirements
- **Responsive Layout**: Mobile-optimized with collapsible filters

---

## Variation 2: Dark Theme Focus Events (Moderate Change)
**Edginess Level**: 3/5 | **Alignment**: Homepage Variation 2

### Design Philosophy
Cyberpunk-inspired dark events page with dramatic neon accents, electric color schemes, and atmospheric effects that embody Salem's alternative rope community aesthetic.

### Visual Characteristics
- **Color Palette**: Deep black (#0D1117) with neon burgundy (#FF0A54) and electric accents
- **Card Design**: Dark cards with glowing neon borders and backdrop effects
- **Typography**: Monospace elements with terminal-style labels
- **Interactions**: Dramatic glow animations with electric color inversions

### Layout Structure
```
+--------------------------------------------------+
|        ████ Dark Header (Neon) ████             |
+--------------------------------------------------+
| > EVENTS_TERMINAL                    [⚡] [☰]    |
| user@witchcity:/events$                          |
+--------------------------------------------------+
| ┌─────────────────┬─────────────────────────────┐|
| │ ╔═══════════╗   │ ████ Event Matrix ████      ││
| │ ║ FILTERS   ║   │ ┌─────┐ ┌─────┐ ┌─────┐    ││
| │ ╚═══════════╝   │ │█EVT█│ │█EVT█│ │█EVT█│    ││
| │ > EVENT_TYPE    │ │ 001 │ │ 002 │ │ 003 │    ││
| │ [◉] WORKSHOPS   │ └─────┘ └─────┘ └─────┘    ││
| │ [◉] PERFORMANCE │ ┌─────┐ ┌─────┐ ┌─────┐    ││
| │ [○] SOCIAL      │ │█EVT█│ │█EVT█│ │█EVT█│    ││
| │ > DATE_RANGE    │ │ 004 │ │ 005 │ │ 006 │    ││
| │ [◉] THIS_WEEK   │ └─────┘ └─────┘ └─────┘    ││
| │ > MEMBER_LVL    │                             ││
| │ [◉] ALL_ACCESS  │ > LOAD_MORE_DATA            ││
| │ [○] VETTED_ONLY │                             ││
| └─────────────────┴─────────────────────────────┘|
+--------------------------------------------------+
```

### Mantine v7 Implementation
```typescript
import { 
  Grid, 
  Card, 
  Image, 
  Text, 
  Badge, 
  Button, 
  Group, 
  Stack, 
  Container,
  Title,
  TextInput,
  Checkbox,
  ActionIcon,
  Box,
  Divider
} from '@mantine/core';

const DarkFocusEvents = () => {
  return (
    <Box
      style={{
        backgroundColor: '#0D1117',
        minHeight: '100vh',
        position: 'relative'
      }}
    >
      {/* Animated grid background */}
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `
            linear-gradient(rgba(255, 10, 84, 0.03) 1px, transparent 1px),
            linear-gradient(90deg, rgba(255, 10, 84, 0.03) 1px, transparent 1px)
          `,
          backgroundSize: '40px 40px',
          animation: 'grid-move 20s linear infinite'
        }}
      />

      {/* Floating neon elements */}
      <Box
        style={{
          position: 'absolute',
          top: '15%',
          right: '10%',
          width: '300px',
          height: '300px',
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(255, 10, 84, 0.08) 0%, transparent 70%)',
          filter: 'blur(60px)',
          animation: 'float 8s ease-in-out infinite'
        }}
      />

      <Container size="xl" px="md" py="lg" style={{ position: 'relative', zIndex: 1 }}>
        {/* Terminal-style Header */}
        <Box mb="xl">
          <Group justify="space-between" align="center" mb="sm">
            <Title
              order={1}
              style={{
                fontFamily: 'monospace',
                color: '#39FF14',
                fontSize: '1.8rem',
                textShadow: '0 0 10px rgba(57, 255, 20, 0.5)',
                letterSpacing: '2px'
              }}
            >
              &gt; EVENTS_TERMINAL
            </Title>
            <Group gap="xs">
              <ActionIcon
                variant="filled"
                style={{
                  backgroundColor: '#FF0A54',
                  boxShadow: '0 0 20px rgba(255, 10, 84, 0.4)'
                }}
              >
                ⚡
              </ActionIcon>
              <ActionIcon
                variant="filled"
                style={{
                  backgroundColor: '#C77DFF',
                  boxShadow: '0 0 20px rgba(199, 125, 255, 0.4)'
                }}
              >
                ☰
              </ActionIcon>
            </Group>
          </Group>
          
          <Text
            style={{
              fontFamily: 'monospace',
              color: '#8B949E',
              fontSize: '0.9rem'
            }}
          >
            user@witchcity:/events$ ls -la --sort=date --filter=upcoming
          </Text>
        </Box>

        <Grid>
          {/* Filters Terminal */}
          <Grid.Col span={{ base: 12, md: 3 }}>
            <Card
              radius="md"
              p="lg"
              style={{
                backgroundColor: '#161B22',
                border: '1px solid #30363D',
                boxShadow: '0 0 20px rgba(255, 10, 84, 0.1)'
              }}
            >
              <Stack gap="lg">
                {/* Terminal Header */}
                <Box>
                  <Text
                    fw="bold"
                    mb="sm"
                    style={{
                      fontFamily: 'monospace',
                      color: '#C77DFF',
                      textTransform: 'uppercase',
                      letterSpacing: '1px',
                      textShadow: '0 0 5px rgba(199, 125, 255, 0.3)'
                    }}
                  >
                    ╔═══════════════╗
                  </Text>
                  <Text
                    fw="bold"
                    mb="sm"
                    style={{
                      fontFamily: 'monospace',
                      color: '#C77DFF',
                      textTransform: 'uppercase',
                      letterSpacing: '1px'
                    }}
                  >
                    ║ FILTER_MODULE ║
                  </Text>
                  <Text
                    fw="bold"
                    mb="lg"
                    style={{
                      fontFamily: 'monospace',
                      color: '#C77DFF',
                      textTransform: 'uppercase',
                      letterSpacing: '1px'
                    }}
                  >
                    ╚═══════════════╝
                  </Text>
                </Box>

                {/* Search Terminal */}
                <Box>
                  <Text
                    size="sm"
                    mb="xs"
                    style={{
                      fontFamily: 'monospace',
                      color: '#39FF14',
                      textTransform: 'uppercase'
                    }}
                  >
                    &gt; SEARCH_QUERY
                  </Text>
                  <TextInput
                    placeholder="search_events..."
                    styles={{
                      input: {
                        backgroundColor: '#0D1117',
                        borderColor: '#30363D',
                        color: '#C9D1D9',
                        fontFamily: 'monospace',
                        '&:focus': {
                          borderColor: '#FF0A54',
                          boxShadow: '0 0 10px rgba(255, 10, 84, 0.3)'
                        },
                        '&::placeholder': {
                          color: '#8B949E',
                          fontFamily: 'monospace'
                        }
                      }
                    }}
                  />
                </Box>

                {/* Event Type Filters */}
                <Box>
                  <Text
                    size="sm"
                    mb="sm"
                    style={{
                      fontFamily: 'monospace',
                      color: '#39FF14',
                      textTransform: 'uppercase'
                    }}
                  >
                    &gt; EVENT_TYPE
                  </Text>
                  <Stack gap="xs">
                    {[
                      { label: 'WORKSHOPS', checked: true },
                      { label: 'PERFORMANCES', checked: true },
                      { label: 'SOCIAL_EVENTS', checked: false },
                      { label: 'EDUCATIONAL', checked: false }
                    ].map(item => (
                      <Group key={item.label} gap="xs">
                        <Text
                          style={{
                            fontFamily: 'monospace',
                            color: item.checked ? '#39FF14' : '#6B7280',
                            fontSize: '0.8rem'
                          }}
                        >
                          [{item.checked ? '◉' : '○'}]
                        </Text>
                        <Text
                          style={{
                            fontFamily: 'monospace',
                            color: item.checked ? '#C9D1D9' : '#6B7280',
                            fontSize: '0.8rem',
                            cursor: 'pointer'
                          }}
                        >
                          {item.label}
                        </Text>
                      </Group>
                    ))}
                  </Stack>
                </Box>

                {/* Date Range */}
                <Box>
                  <Text
                    size="sm"
                    mb="sm"
                    style={{
                      fontFamily: 'monospace',
                      color: '#39FF14',
                      textTransform: 'uppercase'
                    }}
                  >
                    &gt; DATE_RANGE
                  </Text>
                  <Stack gap="xs">
                    {[
                      { label: 'THIS_WEEK', checked: true },
                      { label: 'THIS_MONTH', checked: false },
                      { label: 'NEXT_QUARTER', checked: false }
                    ].map(item => (
                      <Group key={item.label} gap="xs">
                        <Text
                          style={{
                            fontFamily: 'monospace',
                            color: item.checked ? '#39FF14' : '#6B7280',
                            fontSize: '0.8rem'
                          }}
                        >
                          [{item.checked ? '◉' : '○'}]
                        </Text>
                        <Text
                          style={{
                            fontFamily: 'monospace',
                            color: item.checked ? '#C9D1D9' : '#6B7280',
                            fontSize: '0.8rem',
                            cursor: 'pointer'
                          }}
                        >
                          {item.label}
                        </Text>
                      </Group>
                    ))}
                  </Stack>
                </Box>

                {/* Member Level */}
                <Box>
                  <Text
                    size="sm"
                    mb="sm"
                    style={{
                      fontFamily: 'monospace',
                      color: '#39FF14',
                      textTransform: 'uppercase'
                    }}
                  >
                    &gt; MEMBER_LEVEL
                  </Text>
                  <Stack gap="xs">
                    <Group gap="xs">
                      <Text style={{ fontFamily: 'monospace', color: '#39FF14', fontSize: '0.8rem' }}>
                        [◉]
                      </Text>
                      <Text style={{ fontFamily: 'monospace', color: '#C9D1D9', fontSize: '0.8rem' }}>
                        ALL_ACCESS
                      </Text>
                    </Group>
                    <Group gap="xs">
                      <Text style={{ fontFamily: 'monospace', color: '#6B7280', fontSize: '0.8rem' }}>
                        [○]
                      </Text>
                      <Text style={{ fontFamily: 'monospace', color: '#6B7280', fontSize: '0.8rem' }}>
                        VETTED_ONLY
                      </Text>
                    </Group>
                  </Stack>
                </Box>
              </Stack>
            </Card>
          </Grid.Col>

          {/* Events Matrix */}
          <Grid.Col span={{ base: 12, md: 9 }}>
            {/* Terminal Output Header */}
            <Group justify="space-between" mb="lg">
              <Text
                style={{
                  fontFamily: 'monospace',
                  color: '#8B949E',
                  fontSize: '0.9rem'
                }}
              >
                &gt; found 24 events | showing 6 | sort=date_asc
              </Text>
              <Group gap="xs">
                <Badge
                  variant="filled"
                  style={{
                    backgroundColor: '#39FF14',
                    color: '#000',
                    fontFamily: 'monospace',
                    textTransform: 'uppercase'
                  }}
                >
                  LIVE
                </Badge>
              </Group>
            </Group>

            {/* Event Cards Matrix */}
            <Grid>
              {Array.from({length: 6}).map((_, index) => (
                <Grid.Col key={index} span={{ base: 12, sm: 6, lg: 4 }}>
                  <Card
                    radius="md"
                    p="md"
                    style={{
                      backgroundColor: '#161B22',
                      border: '1px solid #FF0A54',
                      boxShadow: '0 0 20px rgba(255, 10, 84, 0.2)',
                      transition: 'all 0.3s ease',
                      cursor: 'pointer',
                      position: 'relative',
                      overflow: 'hidden'
                    }}
                    styles={{
                      root: {
                        '&:hover': {
                          transform: 'translateY(-5px)',
                          boxShadow: '0 0 30px rgba(255, 10, 84, 0.4)',
                          borderColor: '#C77DFF'
                        }
                      }
                    }}
                  >
                    {/* Animated border glow */}
                    <Box
                      style={{
                        position: 'absolute',
                        top: -1,
                        left: -1,
                        right: -1,
                        bottom: -1,
                        background: 'linear-gradient(45deg, #FF0A54, #C77DFF, #39FF14, #FF0A54)',
                        borderRadius: 'inherit',
                        zIndex: -1,
                        opacity: 0,
                        animation: 'border-pulse 3s ease-in-out infinite'
                      }}
                    />

                    {/* Event ID Header */}
                    <Group justify="space-between" mb="sm">
                      <Text
                        style={{
                          fontFamily: 'monospace',
                          color: '#39FF14',
                          fontSize: '0.8rem',
                          textShadow: '0 0 5px rgba(57, 255, 20, 0.3)'
                        }}
                      >
                        █EVT_00{index + 1}█
                      </Text>
                      <Badge
                        variant="filled"
                        size="xs"
                        style={{
                          backgroundColor: '#FF0A54',
                          fontFamily: 'monospace'
                        }}
                      >
                        WORKSHOP
                      </Badge>
                    </Group>

                    {/* Event Image with Glitch Effect */}
                    <Box
                      style={{
                        position: 'relative',
                        marginBottom: '1rem'
                      }}
                    >
                      <Image
                        src={`https://picsum.photos/400/200?random=${index}`}
                        height={160}
                        radius="sm"
                        style={{
                          filter: 'contrast(1.2) brightness(0.8)'
                        }}
                      />
                      <Box
                        style={{
                          position: 'absolute',
                          top: 0,
                          left: 0,
                          right: 0,
                          bottom: 0,
                          background: 'linear-gradient(45deg, rgba(255, 10, 84, 0.1), rgba(199, 125, 255, 0.1))',
                          borderRadius: '4px'
                        }}
                      />
                    </Box>

                    {/* Event Content */}
                    <Stack gap="sm">
                      <div>
                        <Text
                          fw="bold"
                          size="md"
                          lineClamp={2}
                          mb="xs"
                          style={{
                            color: '#C9D1D9',
                            textShadow: '0 0 5px rgba(201, 209, 217, 0.3)'
                          }}
                        >
                          ADVANCED_SUSPENSION.exe
                        </Text>
                        <Text
                          size="sm"
                          c="dimmed"
                          lineClamp={2}
                          style={{
                            fontFamily: 'monospace',
                            color: '#8B949E'
                          }}
                        >
                          // Master advanced suspension protocols
                          // Safety modules included
                        </Text>
                      </div>

                      {/* Terminal-style Details */}
                      <Stack gap="xs">
                        <Group gap="xs">
                          <Text
                            size="xs"
                            style={{
                              fontFamily: 'monospace',
                              color: '#39FF14'
                            }}
                          >
                            DATE:
                          </Text>
                          <Text
                            size="xs"
                            style={{
                              fontFamily: 'monospace',
                              color: '#C9D1D9'
                            }}
                          >
                            2024-12-14_14:00-17:00
                          </Text>
                        </Group>
                        
                        <Group gap="xs">
                          <Text
                            size="xs"
                            style={{
                              fontFamily: 'monospace',
                              color: '#39FF14'
                            }}
                          >
                            LOCATION:
                          </Text>
                          <Text
                            size="xs"
                            style={{
                              fontFamily: 'monospace',
                              color: '#C9D1D9'
                            }}
                          >
                            salem_arts_center.venue
                          </Text>
                        </Group>

                        <Group gap="xs">
                          <Text
                            size="xs"
                            style={{
                              fontFamily: 'monospace',
                              color: '#39FF14'
                            }}
                          >
                            INSTRUCTOR:
                          </Text>
                          <Text
                            size="xs"
                            style={{
                              fontFamily: 'monospace',
                              color: '#C9D1D9'
                            }}
                          >
                            sarah_mitchell.admin
                          </Text>
                        </Group>

                        <Group gap="xs">
                          <Text
                            size="xs"
                            style={{
                              fontFamily: 'monospace',
                              color: '#39FF14'
                            }}
                          >
                            CAPACITY:
                          </Text>
                          <Text
                            size="xs"
                            style={{
                              fontFamily: 'monospace',
                              color: '#FFB000'
                            }}
                          >
                            [████████░░] 8/12
                          </Text>
                        </Group>
                      </Stack>

                      {/* Price and Action */}
                      <Group justify="space-between" align="center" mt="sm">
                        <div>
                          <Text
                            fw="bold"
                            size="lg"
                            style={{
                              color: '#39FF14',
                              fontFamily: 'monospace',
                              textShadow: '0 0 5px rgba(57, 255, 20, 0.3)'
                            }}
                          >
                            $85.00
                          </Text>
                          <Text
                            size="xs"
                            style={{
                              color: '#8B949E',
                              fontFamily: 'monospace'
                            }}
                          >
                            sliding_scale=true
                          </Text>
                        </div>
                        <Button
                          size="sm"
                          style={{
                            background: 'linear-gradient(135deg, #FF0A54 0%, #C77DFF 100%)',
                            border: 'none',
                            boxShadow: '0 0 15px rgba(255, 10, 84, 0.4)',
                            fontFamily: 'monospace',
                            textTransform: 'uppercase',
                            letterSpacing: '1px'
                          }}
                          styles={{
                            root: {
                              '&:hover': {
                                transform: 'translateY(-2px)',
                                boxShadow: '0 0 25px rgba(255, 10, 84, 0.6)'
                              }
                            }
                          }}
                        >
                          &gt; EXECUTE
                        </Button>
                      </Group>

                      {/* Access Level */}
                      <Badge
                        variant="filled"
                        size="xs"
                        style={{
                          backgroundColor: '#0D1117',
                          border: '1px solid #30363D',
                          color: '#39FF14',
                          fontFamily: 'monospace',
                          alignSelf: 'flex-start'
                        }}
                      >
                        ACCESS_LEVEL: VETTED
                      </Badge>
                    </Stack>
                  </Card>
                </Grid.Col>
              ))}
            </Grid>

            {/* Load More Terminal */}
            <Group justify="center" mt="xl">
              <Button
                variant="outline"
                size="lg"
                style={{
                  borderColor: '#FF0A54',
                  color: '#FF0A54',
                  fontFamily: 'monospace',
                  textTransform: 'uppercase',
                  letterSpacing: '2px',
                  boxShadow: '0 0 15px rgba(255, 10, 84, 0.2)'
                }}
                styles={{
                  root: {
                    '&:hover': {
                      backgroundColor: 'rgba(255, 10, 84, 0.1)',
                      boxShadow: '0 0 25px rgba(255, 10, 84, 0.4)'
                    }
                  }
                }}
              >
                &gt; LOAD_MORE_DATA
              </Button>
            </Group>
          </Grid.Col>
        </Grid>
      </Container>

      {/* CSS Animations */}
      <style>{`
        @keyframes grid-move {
          0% { transform: translate(0, 0); }
          100% { transform: translate(40px, 40px); }
        }
        
        @keyframes float {
          0%, 100% { transform: translateY(0px); }
          50% { transform: translateY(-20px); }
        }
        
        @keyframes border-pulse {
          0%, 100% { opacity: 0; }
          50% { opacity: 1; }
        }
      `}</style>
    </Box>
  );
};
```

### Unique Features
- **Terminal Interface**: Command-line aesthetic with monospace typography
- **Matrix-Style Layout**: Grid pattern with ID-based event numbering
- **Neon Glow Effects**: Electric colors with pulsing border animations
- **Cyberpunk Details**: Terminal commands, progress bars, access levels
- **Atmospheric Background**: Animated grid and floating neon orbs
- **Electric Interactions**: Dramatic hover effects with color inversions

---

## Variation 3: Geometric Modern Events (Significant Shift)
**Edginess Level**: 4/5 | **Alignment**: Homepage Variation 3

### Design Philosophy
Bold geometric patterns with asymmetric layouts, sharp angular design elements, and innovative use of CSS clip-path to create a distinctive modern events discovery experience.

### Visual Characteristics
- **Color Palette**: High contrast with bold accent colors and geometric shapes
- **Card Design**: Asymmetric shapes with clip-path styling and angular elements
- **Typography**: Bold, minimal typography with geometric emphasis
- **Interactions**: Sharp, angular animations with geometric transforms

### Layout Structure
```
+--------------------------------------------------+
|        ▲ Geometric Header ▲                     |
+--------------------------------------------------+
| EVENTS                                   ◆ ◇ ▲   |
| ▶ Salem's Community Calendar                     |
+--------------------------------------------------+
| ┌─────────────┬─────────────────────────────────┐|
| │ ◆ FILTERS   │ ╱╲ Event Geometry ╱╲           ││
| │ ▶ Type      │ ┌─────┐   ┌─────┐   ┌─────┐   ││
| │ □ Workshop  │ │ ╱───╲ │ │ ╱───╲ │ │ ╱───╲ │ ││
| │ □ Social    │ │╱ EVT ╲│ │╱ EVT ╲│ │╱ EVT ╲│ ││
| │ ▶ Date      │ │╲  1  ╱│ │╲  2  ╱│ │╲  3  ╱│ ││
| │ ◆ This Week │ │ ╲───╱ │ │ ╲───╱ │ │ ╲───╱ │ ││
| │ ◇ This Month│ └─────┘   └─────┘   └─────┘   ││
| │ ▶ Level     │ ╱╲      ╱╲      ╱╲            ││
| │ ◆ All       │┌───┐   ┌───┐   ┌───┐          ││
| │ ◇ Vetted    ││EVT│   │EVT│   │EVT│          ││
| └─────────────┴─────────────────────────────────┘|
+--------------------------------------------------+
```

### Mantine v7 Implementation
```typescript
import { 
  Grid, 
  Card, 
  Image, 
  Text, 
  Badge, 
  Button, 
  Group, 
  Stack, 
  Container,
  Title,
  TextInput,
  ActionIcon,
  Box,
  Avatar
} from '@mantine/core';

const GeometricEvents = () => {
  return (
    <Box
      style={{
        background: 'linear-gradient(135deg, #F8F9FA 0%, #E9ECEF 100%)',
        minHeight: '100vh',
        position: 'relative'
      }}
    >
      {/* Geometric background elements */}
      <Box
        style={{
          position: 'absolute',
          top: '10%',
          right: '15%',
          width: '200px',
          height: '200px',
          background: 'linear-gradient(45deg, #880124, #B76D75)',
          clipPath: 'polygon(0 0, 100% 0, 80% 100%, 0 85%)',
          opacity: 0.05
        }}
      />
      
      <Box
        style={{
          position: 'absolute',
          bottom: '20%',
          left: '5%',
          width: '150px',
          height: '150px',
          background: 'linear-gradient(135deg, #9D4EDD, #C77DFF)',
          clipPath: 'polygon(20% 0%, 100% 20%, 80% 100%, 0% 80%)',
          opacity: 0.05
        }}
      />

      <Container size="xl" px="md" py="lg" style={{ position: 'relative', zIndex: 1 }}>
        {/* Geometric Header */}
        <Box mb="xl">
          <Group justify="space-between" align="center" mb="lg">
            <Box>
              <Title
                order={1}
                size="3.5rem"
                fw="900"
                lh={0.8}
                mb="sm"
                style={{
                  color: '#880124',
                  letterSpacing: '-3px',
                  textTransform: 'uppercase'
                }}
              >
                EVENTS
              </Title>
              <Text
                size="xl"
                fw={600}
                style={{
                  color: '#B76D75',
                  textTransform: 'uppercase',
                  letterSpacing: '2px'
                }}
              >
                ▶ Salem's Community Calendar
              </Text>
            </Box>
            
            <Group gap="lg">
              <Box
                style={{
                  width: '60px',
                  height: '60px',
                  background: 'linear-gradient(45deg, #880124, #B76D75)',
                  clipPath: 'polygon(50% 0%, 100% 50%, 50% 100%, 0% 50%)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center'
                }}
              >
                <Text fw="bold" c="white">◆</Text>
              </Box>
              <Box
                style={{
                  width: '50px',
                  height: '50px',
                  background: 'linear-gradient(135deg, #9D4EDD, #C77DFF)',
                  clipPath: 'polygon(30% 0%, 100% 0%, 70% 100%, 0% 100%)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center'
                }}
              >
                <Text fw="bold" c="white">◇</Text>
              </Box>
              <Box
                style={{
                  width: '40px',
                  height: '40px',
                  background: 'linear-gradient(90deg, #880124, #9D4EDD)',
                  clipPath: 'polygon(50% 0%, 0% 100%, 100% 100%)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center'
                }}
              >
                <Text fw="bold" c="white" size="xs">▲</Text>
              </Box>
            </Group>
          </Group>
        </Box>

        <Grid>
          {/* Geometric Filters */}
          <Grid.Col span={{ base: 12, md: 3 }}>
            <Card
              radius={0}
              p="xl"
              style={{
                clipPath: 'polygon(0 0, 100% 0, 90% 100%, 0 100%)',
                backgroundColor: 'white',
                border: '3px solid #880124',
                borderLeft: '8px solid #880124'
              }}
            >
              <Stack gap="xl">
                {/* Filters Header */}
                <Box>
                  <Text
                    fw="900"
                    size="xl"
                    mb="md"
                    style={{
                      color: '#880124',
                      textTransform: 'uppercase',
                      letterSpacing: '1px'
                    }}
                  >
                    ◆ FILTERS
                  </Text>
                </Box>

                {/* Event Type */}
                <Box>
                  <Text
                    fw="bold"
                    mb="md"
                    style={{
                      color: '#880124',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    ▶ EVENT TYPE
                  </Text>
                  <Stack gap="sm">
                    {[
                      { label: 'WORKSHOPS', shape: '□', selected: true },
                      { label: 'PERFORMANCES', shape: '◇', selected: false },
                      { label: 'SOCIAL', shape: '○', selected: false },
                      { label: 'EDUCATIONAL', shape: '△', selected: true }
                    ].map(item => (
                      <Group key={item.label} gap="sm">
                        <Text
                          fw="bold"
                          style={{
                            color: item.selected ? '#880124' : '#9CA3AF',
                            fontSize: '1.2rem'
                          }}
                        >
                          {item.shape}
                        </Text>
                        <Text
                          fw={item.selected ? 'bold' : 'normal'}
                          style={{
                            color: item.selected ? '#1F2937' : '#9CA3AF',
                            textTransform: 'uppercase',
                            letterSpacing: '0.5px',
                            cursor: 'pointer'
                          }}
                        >
                          {item.label}
                        </Text>
                      </Group>
                    ))}
                  </Stack>
                </Box>

                {/* Date Range */}
                <Box>
                  <Text
                    fw="bold"
                    mb="md"
                    style={{
                      color: '#880124',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    ▶ DATE RANGE
                  </Text>
                  <Stack gap="sm">
                    {[
                      { label: 'THIS WEEK', shape: '◆', selected: true },
                      { label: 'THIS MONTH', shape: '◇', selected: false },
                      { label: 'NEXT QUARTER', shape: '▲', selected: false }
                    ].map(item => (
                      <Group key={item.label} gap="sm">
                        <Text
                          fw="bold"
                          style={{
                            color: item.selected ? '#B76D75' : '#9CA3AF',
                            fontSize: '1rem'
                          }}
                        >
                          {item.shape}
                        </Text>
                        <Text
                          fw={item.selected ? 'bold' : 'normal'}
                          style={{
                            color: item.selected ? '#1F2937' : '#9CA3AF',
                            textTransform: 'uppercase',
                            letterSpacing: '0.5px',
                            cursor: 'pointer'
                          }}
                        >
                          {item.label}
                        </Text>
                      </Group>
                    ))}
                  </Stack>
                </Box>

                {/* Member Level */}
                <Box>
                  <Text
                    fw="bold"
                    mb="md"
                    style={{
                      color: '#880124',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    ▶ ACCESS LEVEL
                  </Text>
                  <Stack gap="sm">
                    <Group gap="sm">
                      <Text fw="bold" style={{ color: '#880124', fontSize: '1rem' }}>◆</Text>
                      <Text fw="bold" style={{ color: '#1F2937', textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                        ALL MEMBERS
                      </Text>
                    </Group>
                    <Group gap="sm">
                      <Text fw="bold" style={{ color: '#9CA3AF', fontSize: '1rem' }}>◇</Text>
                      <Text style={{ color: '#9CA3AF', textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                        VETTED ONLY
                      </Text>
                    </Group>
                  </Stack>
                </Box>
              </Stack>
            </Card>
          </Grid.Col>

          {/* Geometric Events Grid */}
          <Grid.Col span={{ base: 12, md: 9 }}>
            {/* Status Header */}
            <Group justify="space-between" mb="xl">
              <Text
                fw="bold"
                style={{
                  color: '#6B7280',
                  textTransform: 'uppercase',
                  letterSpacing: '1px'
                }}
              >
                ▶ DISPLAYING 24 OF 156 EVENTS
              </Text>
              <Group gap="sm">
                <ActionIcon
                  variant="filled"
                  color="red"
                  size="lg"
                  style={{
                    clipPath: 'polygon(50% 0%, 100% 50%, 50% 100%, 0% 50%)'
                  }}
                >
                  ◆
                </ActionIcon>
                <ActionIcon
                  variant="outline"
                  color="red"
                  size="lg"
                  style={{
                    clipPath: 'polygon(30% 0%, 100% 0%, 70% 100%, 0% 100%)'
                  }}
                >
                  ◇
                </ActionIcon>
              </Group>
            </Group>

            {/* Geometric Event Cards */}
            <Grid>
              {Array.from({length: 6}).map((_, index) => (
                <Grid.Col key={index} span={{ base: 12, sm: 6, lg: 4 }}>
                  <Card
                    radius={0}
                    shadow="lg"
                    p="lg"
                    style={{
                      clipPath: index % 2 === 0 
                        ? 'polygon(0 0, 100% 0, 100% 85%, 15% 100%, 0 100%)' 
                        : 'polygon(0 0, 85% 0, 100% 15%, 100% 100%, 0 100%)',
                      backgroundColor: 'white',
                      border: `3px solid ${index % 3 === 0 ? '#880124' : index % 3 === 1 ? '#B76D75' : '#9D4EDD'}`,
                      transition: 'all 0.4s ease',
                      cursor: 'pointer'
                    }}
                    styles={{
                      root: {
                        '&:hover': {
                          transform: 'translateY(-8px) rotateX(5deg)',
                          boxShadow: '0 20px 40px rgba(136, 1, 36, 0.2)'
                        }
                      }
                    }}
                  >
                    {/* Event Header */}
                    <Group justify="space-between" mb="md">
                      <Text
                        fw="900"
                        style={{
                          color: '#880124',
                          fontSize: '0.8rem',
                          textTransform: 'uppercase',
                          letterSpacing: '1px'
                        }}
                      >
                        ▶ EVENT_00{index + 1}
                      </Text>
                      <Badge
                        variant="filled"
                        color="red"
                        style={{
                          clipPath: 'polygon(10% 0%, 100% 0%, 90% 100%, 0% 100%)',
                          fontWeight: 'bold',
                          textTransform: 'uppercase'
                        }}
                      >
                        WORKSHOP
                      </Badge>
                    </Group>

                    {/* Geometric Image Container */}
                    <Box
                      style={{
                        position: 'relative',
                        marginBottom: '1rem'
                      }}
                    >
                      <Image
                        src={`https://picsum.photos/400/200?random=${index}`}
                        height={180}
                        style={{
                          clipPath: 'polygon(0 0, 100% 0, 90% 100%, 0 100%)'
                        }}
                      />
                      <Box
                        style={{
                          position: 'absolute',
                          top: 12,
                          right: 12,
                          width: '40px',
                          height: '40px',
                          background: 'linear-gradient(45deg, #880124, #B76D75)',
                          clipPath: 'polygon(50% 0%, 100% 50%, 50% 100%, 0% 50%)',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center'
                        }}
                      >
                        <Text fw="bold" c="white" size="sm">◆</Text>
                      </Box>
                    </Box>

                    {/* Event Content */}
                    <Stack gap="md">
                      <div>
                        <Title
                          order={4}
                          fw="900"
                          size="lg"
                          lineClamp={2}
                          mb="xs"
                          style={{
                            color: '#1F2937',
                            textTransform: 'uppercase',
                            letterSpacing: '0.5px'
                          }}
                        >
                          ADVANCED SUSPENSION
                        </Title>
                        <Text
                          size="sm"
                          c="dimmed"
                          lineClamp={2}
                          fw={500}
                        >
                          Master suspension techniques with expert safety protocols and artistic guidance.
                        </Text>
                      </div>

                      {/* Geometric Details */}
                      <Stack gap="sm">
                        <Group gap="sm">
                          <Box
                            style={{
                              width: '20px',
                              height: '20px',
                              background: '#B76D75',
                              clipPath: 'polygon(50% 0%, 100% 50%, 50% 100%, 0% 50%)'
                            }}
                          />
                          <Text fw="bold" size="sm" style={{ textTransform: 'uppercase' }}>
                            SAT, DEC 14 • 2:00-5:00 PM
                          </Text>
                        </Group>
                        
                        <Group gap="sm">
                          <Box
                            style={{
                              width: '20px',
                              height: '20px',
                              background: '#9D4EDD',
                              clipPath: 'polygon(30% 0%, 100% 0%, 70% 100%, 0% 100%)'
                            }}
                          />
                          <Text fw="bold" size="sm" style={{ textTransform: 'uppercase' }}>
                            SALEM ARTS CENTER
                          </Text>
                        </Group>

                        <Group gap="sm">
                          <Avatar size="sm" style={{ clipPath: 'polygon(50% 0%, 100% 50%, 50% 100%, 0% 50%)' }}>
                            SM
                          </Avatar>
                          <Text fw="bold" size="sm" style={{ textTransform: 'uppercase' }}>
                            SARAH MITCHELL
                          </Text>
                        </Group>

                        <Group gap="sm">
                          <Box
                            style={{
                              width: '20px',
                              height: '20px',
                              background: '#39FF14',
                              clipPath: 'polygon(50% 0%, 0% 100%, 100% 100%)'
                            }}
                          />
                          <Text fw="bold" size="sm" style={{ textTransform: 'uppercase' }}>
                            8 OF 12 SPOTS FILLED
                          </Text>
                        </Group>
                      </Stack>

                      {/* Price and Action */}
                      <Group justify="space-between" align="center" mt="md">
                        <div>
                          <Text
                            fw="900"
                            size="xl"
                            style={{
                              color: '#880124',
                              textTransform: 'uppercase'
                            }}
                          >
                            $85
                          </Text>
                          <Text
                            size="xs"
                            fw="bold"
                            style={{
                              color: '#6B7280',
                              textTransform: 'uppercase'
                            }}
                          >
                            SLIDING SCALE
                          </Text>
                        </div>
                        <Button
                          size="md"
                          style={{
                            backgroundColor: '#880124',
                            border: 'none',
                            clipPath: 'polygon(0 0, calc(100% - 15px) 0, 100% 100%, 15px 100%)',
                            fontWeight: 'bold',
                            textTransform: 'uppercase',
                            letterSpacing: '1px'
                          }}
                          styles={{
                            root: {
                              '&:hover': {
                                backgroundColor: '#B76D75',
                                transform: 'translateX(3px)'
                              }
                            }
                          }}
                        >
                          ▶ REGISTER
                        </Button>
                      </Group>

                      {/* Access Badge */}
                      <Badge
                        variant="outline"
                        color="blue"
                        size="sm"
                        style={{
                          clipPath: 'polygon(15% 0%, 100% 0%, 85% 100%, 0% 100%)',
                          alignSelf: 'flex-start',
                          fontWeight: 'bold',
                          textTransform: 'uppercase'
                        }}
                      >
                        ◆ VETTED ACCESS
                      </Badge>
                    </Stack>
                  </Card>
                </Grid.Col>
              ))}
            </Grid>

            {/* Load More Geometric */}
            <Group justify="center" mt="xl">
              <Button
                variant="outline"
                size="xl"
                style={{
                  borderColor: '#880124',
                  color: '#880124',
                  clipPath: 'polygon(20px 0%, 100% 0%, calc(100% - 20px) 100%, 0% 100%)',
                  fontWeight: 'bold',
                  textTransform: 'uppercase',
                  letterSpacing: '2px',
                  padding: '1rem 3rem'
                }}
                styles={{
                  root: {
                    '&:hover': {
                      backgroundColor: 'rgba(136, 1, 36, 0.1)',
                      transform: 'translateY(-3px)'
                    }
                  }
                }}
              >
                ▶ LOAD MORE EVENTS
              </Button>
            </Group>
          </Grid.Col>
        </Grid>
      </Container>
    </Box>
  );
};
```

### Unique Features
- **Clip-Path Mastery**: Angular event cards with asymmetric shapes
- **Geometric Icons**: Shape-based symbols for filtering and navigation
- **Angular Typography**: Bold, uppercase text with geometric emphasis
- **Asymmetric Grid**: Intentionally broken grid layouts for visual interest
- **Shape-Based UI Elements**: Diamonds, triangles, and polygons throughout
- **3D Hover Effects**: Perspective transforms on card interactions

---

## Variation 4: Advanced Mantine Events (Dramatic Change)
**Edginess Level**: 4/5 | **Alignment**: Homepage Variation 4

### Design Philosophy
Leverages advanced Mantine v7 components with sophisticated interactions, rich data visualization, modern UX patterns, and comprehensive event management features.

### Visual Characteristics
- **Component Richness**: Advanced Mantine features like DataTable, Charts, Spotlight
- **Data Visualization**: Event analytics, capacity tracking, popularity metrics
- **Interactive Elements**: Rich hover states, complex animations, advanced filtering
- **Professional UX**: Enterprise-grade interface patterns and workflows

### Mantine v7 Implementation
```typescript
import { 
  Grid, 
  Card, 
  Image, 
  Text, 
  Badge, 
  Button, 
  Group, 
  Stack, 
  Container,
  Title,
  TextInput,
  Select,
  MultiSelect,
  DatePicker,
  ActionIcon,
  Tooltip,
  Avatar,
  Progress,
  Tabs,
  DataTable,
  Spotlight,
  Menu,
  Notification,
  Modal,
  Stepper,
  Timeline,
  AreaChart,
  PieChart,
  Drawer,
  Skeleton
} from '@mantine/core';
import { 
  IconSearch, 
  IconFilter, 
  IconCalendar, 
  IconMapPin, 
  IconUsers, 
  IconHeart, 
  IconShare,
  IconChartBar,
  IconSettings,
  IconBookmark,
  IconEye,
  IconPlus,
  IconArrowRight,
  IconStar,
  IconTrendingUp
} from '@tabler/icons-react';
import { useDisclosure } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';

const AdvancedMantineEvents = () => {
  const [viewMode, setViewMode] = useState('grid');
  const [filtersOpened, { open: openFilters, close: closeFilters }] = useDisclosure(false);
  const [selectedEvents, setSelectedEvents] = useState([]);
  const [loading, setLoading] = useState(false);

  // Advanced search and filtering state
  const [searchQuery, setSearchQuery] = useState('');
  const [eventTypes, setEventTypes] = useState([]);
  const [skillLevels, setSkillLevels] = useState([]);
  const [dateRange, setDateRange] = useState([null, null]);
  const [priceRange, setPriceRange] = useState([0, 200]);

  return (
    <Container size="xl" px="md" py="lg">
      {/* Advanced Header with Spotlight Integration */}
      <Group justify="space-between" mb="xl">
        <div>
          <Title order={1} size="2.5rem" fw="bold" mb="xs">
            Community Events
          </Title>
          <Text size="lg" c="dimmed">
            Discover, filter, and register for Salem's premier rope bondage events
          </Text>
        </div>
        
        <Group gap="sm">
          <Tooltip label="Global search (Ctrl+K)">
            <ActionIcon
              variant="gradient"
              gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}
              size="xl"
              onClick={() => Spotlight.open()}
            >
              <IconSearch size={20} />
            </ActionIcon>
          </Tooltip>
          
          <Menu shadow="xl" width={250}>
            <Menu.Target>
              <ActionIcon variant="light" color="witchcity" size="xl">
                <IconSettings size={20} />
              </ActionIcon>
            </Menu.Target>
            <Menu.Dropdown>
              <Menu.Label>Event Views</Menu.Label>
              <Menu.Item leftSection={<IconChartBar size={14} />}>
                Analytics Dashboard
              </Menu.Item>
              <Menu.Item leftSection={<IconCalendar size={14} />}>
                Calendar View
              </Menu.Item>
              <Menu.Item leftSection={<IconUsers size={14} />}>
                Instructor Directory
              </Menu.Item>
              <Menu.Divider />
              <Menu.Label>Preferences</Menu.Label>
              <Menu.Item leftSection={<IconBookmark size={14} />}>
                Saved Events
              </Menu.Item>
              <Menu.Item leftSection={<IconEye size={14} />}>
                Recently Viewed
              </Menu.Item>
            </Menu.Dropdown>
          </Menu>
        </Group>
      </Group>

      {/* Advanced Analytics Panel */}
      <Card shadow="md" radius="lg" p="lg" mb="xl" withBorder>
        <Group justify="space-between" mb="lg">
          <Title order={3}>Event Analytics</Title>
          <Badge variant="light" color="green" leftSection={<IconTrendingUp size={14} />}>
            Live Data
          </Badge>
        </Group>
        
        <Grid>
          <Grid.Col span={{ base: 6, sm: 3 }}>
            <Card withBorder radius="md" p="md">
              <Group>
                <Avatar color="witchcity" radius="md">
                  <IconCalendar size={20} />
                </Avatar>
                <div>
                  <Text fw="bold" size="xl">156</Text>
                  <Text size="sm" c="dimmed">Total Events</Text>
                </div>
              </Group>
            </Card>
          </Grid.Col>
          
          <Grid.Col span={{ base: 6, sm: 3 }}>
            <Card withBorder radius="md" p="md">
              <Group>
                <Avatar color="blue" radius="md">
                  <IconUsers size={20} />
                </Avatar>
                <div>
                  <Text fw="bold" size="xl">1,247</Text>
                  <Text size="sm" c="dimmed">Total RSVPs</Text>
                </div>
              </Group>
            </Card>
          </Grid.Col>
          
          <Grid.Col span={{ base: 6, sm: 3 }}>
            <Card withBorder radius="md" p="md">
              <Group>
                <Avatar color="green" radius="md">
                  <IconStar size={20} />
                </Avatar>
                <div>
                  <Text fw="bold" size="xl">4.8</Text>
                  <Text size="sm" c="dimmed">Avg Rating</Text>
                </div>
              </Group>
            </Card>
          </Grid.Col>
          
          <Grid.Col span={{ base: 6, sm: 3 }}>
            <Card withBorder radius="md" p="md">
              <Group>
                <Avatar color="orange" radius="md">
                  <IconTrendingUp size={20} />
                </Avatar>
                <div>
                  <Text fw="bold" size="xl">23%</Text>
                  <Text size="sm" c="dimmed">Growth</Text>
                </div>
              </Group>
            </Card>
          </Grid.Col>
        </Grid>
      </Card>

      {/* Advanced Tabs Interface */}
      <Tabs defaultValue="events" mb="xl">
        <Tabs.List grow>
          <Tabs.Tab value="events" leftSection={<IconCalendar size={16} />}>
            Event Browser
          </Tabs.Tab>
          <Tabs.Tab value="calendar" leftSection={<IconChartBar size={16} />}>
            Calendar View
          </Tabs.Tab>
          <Tabs.Tab value="analytics" leftSection={<IconTrendingUp size={16} />}>
            Analytics
          </Tabs.Tab>
          <Tabs.Tab value="saved" leftSection={<IconBookmark size={16} />}>
            Saved Events
          </Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="events" pt="lg">
          <Grid>
            {/* Advanced Filters Sidebar */}
            <Grid.Col span={{ base: 12, md: 3 }}>
              <Card shadow="sm" radius="lg" p="lg" withBorder>
                <Stack gap="lg">
                  <div>
                    <Group justify="space-between" mb="sm">
                      <Text fw={600}>Smart Filters</Text>
                      <ActionIcon variant="subtle" size="sm">
                        <IconFilter size={14} />
                      </ActionIcon>
                    </Group>
                    
                    <TextInput
                      placeholder="Search events, instructors..."
                      leftSection={<IconSearch size={16} />}
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      mb="md"
                    />
                  </div>

                  <div>
                    <Text fw={600} mb="sm">Event Types</Text>
                    <MultiSelect
                      placeholder="Select types"
                      data={[
                        { value: 'workshop', label: 'Workshops' },
                        { value: 'performance', label: 'Performances' },
                        { value: 'social', label: 'Social Events' },
                        { value: 'educational', label: 'Educational' },
                        { value: 'competition', label: 'Competitions' }
                      ]}
                      value={eventTypes}
                      onChange={setEventTypes}
                      searchable
                      clearable
                    />
                  </div>

                  <div>
                    <Text fw={600} mb="sm">Skill Levels</Text>
                    <MultiSelect
                      placeholder="Select levels"
                      data={[
                        { value: 'beginner', label: 'Beginner' },
                        { value: 'intermediate', label: 'Intermediate' },
                        { value: 'advanced', label: 'Advanced' },
                        { value: 'expert', label: 'Expert' },
                        { value: 'all', label: 'All Levels' }
                      ]}
                      value={skillLevels}
                      onChange={setSkillLevels}
                    />
                  </div>

                  <div>
                    <Text fw={600} mb="sm">Date Range</Text>
                    <DatePicker
                      type="range"
                      placeholder="Select date range"
                      value={dateRange}
                      onChange={setDateRange}
                    />
                  </div>

                  <div>
                    <Text fw={600} mb="sm">Instructor</Text>
                    <Select
                      placeholder="Filter by instructor"
                      data={[
                        { value: 'sarah', label: 'Sarah Mitchell' },
                        { value: 'alex', label: 'Alex Chen' },
                        { value: 'jamie', label: 'Jamie Rodriguez' },
                        { value: 'morgan', label: 'Morgan Taylor' }
                      ]}
                      searchable
                      clearable
                    />
                  </div>

                  <div>
                    <Text fw={600} mb="sm">Member Access</Text>
                    <Select
                      placeholder="Access level"
                      data={[
                        { value: 'all', label: 'All Members' },
                        { value: 'vetted', label: 'Vetted Only' },
                        { value: 'premium', label: 'Premium Members' }
                      ]}
                      defaultValue="all"
                    />
                  </div>

                  <Button variant="light" color="witchcity" fullWidth>
                    Apply Filters
                  </Button>
                </Stack>
              </Card>
            </Grid.Col>

            {/* Events Grid with Advanced Features */}
            <Grid.Col span={{ base: 12, md: 9 }}>
              <Group justify="space-between" mb="lg">
                <Group gap="sm">
                  <Text c="dimmed">Showing 24 of 156 events</Text>
                  {selectedEvents.length > 0 && (
                    <Badge variant="filled" color="witchcity">
                      {selectedEvents.length} selected
                    </Badge>
                  )}
                </Group>
                
                <Group gap="xs">
                  <Tooltip label="Grid view">
                    <ActionIcon 
                      variant={viewMode === 'grid' ? 'filled' : 'light'} 
                      color="witchcity"
                      onClick={() => setViewMode('grid')}
                    >
                      ⊞
                    </ActionIcon>
                  </Tooltip>
                  <Tooltip label="List view">
                    <ActionIcon 
                      variant={viewMode === 'list' ? 'filled' : 'light'} 
                      color="witchcity"
                      onClick={() => setViewMode('list')}
                    >
                      ☰
                    </ActionIcon>
                  </Tooltip>
                  <Tooltip label="Table view">
                    <ActionIcon 
                      variant={viewMode === 'table' ? 'filled' : 'light'} 
                      color="witchcity"
                      onClick={() => setViewMode('table')}
                    >
                      ≡
                    </ActionIcon>
                  </Tooltip>
                </Group>
              </Group>

              {/* Grid View */}
              {viewMode === 'grid' && (
                <Grid>
                  {Array.from({length: 6}).map((_, index) => (
                    <Grid.Col key={index} span={{ base: 12, sm: 6, lg: 4 }}>
                      <Card
                        shadow="md"
                        radius="lg"
                        withBorder
                        padding="lg"
                        style={{
                          transition: 'all 0.3s ease',
                          cursor: 'pointer',
                          position: 'relative'
                        }}
                        styles={{
                          root: {
                            '&:hover': {
                              transform: 'translateY(-8px)',
                              boxShadow: '0 20px 40px rgba(136, 1, 36, 0.15)'
                            }
                          }
                        }}
                      >
                        {/* Event Image with Overlay */}
                        <Card.Section>
                          <Box style={{ position: 'relative' }}>
                            <Image
                              src={`https://picsum.photos/400/200?random=${index}`}
                              height={200}
                              alt="Event image"
                            />
                            
                            {/* Overlay with Quick Actions */}
                            <Box
                              style={{
                                position: 'absolute',
                                top: 0,
                                left: 0,
                                right: 0,
                                bottom: 0,
                                background: 'linear-gradient(to bottom, transparent 0%, rgba(0,0,0,0.7) 100%)',
                                display: 'flex',
                                alignItems: 'flex-end',
                                padding: '1rem',
                                opacity: 0,
                                transition: 'opacity 0.3s ease'
                              }}
                              className="event-overlay"
                            >
                              <Group gap="xs">
                                <ActionIcon variant="filled" color="red" size="sm">
                                  <IconHeart size={14} />
                                </ActionIcon>
                                <ActionIcon variant="filled" color="blue" size="sm">
                                  <IconShare size={14} />
                                </ActionIcon>
                                <ActionIcon variant="filled" color="green" size="sm">
                                  <IconBookmark size={14} />
                                </ActionIcon>
                              </Group>
                            </Box>

                            {/* Badges */}
                            <Badge
                              variant="filled"
                              color="witchcity"
                              style={{
                                position: 'absolute',
                                top: 12,
                                left: 12
                              }}
                            >
                              Workshop
                            </Badge>
                            
                            <Badge
                              variant="filled"
                              color="green"
                              style={{
                                position: 'absolute',
                                top: 12,
                                right: 12
                              }}
                            >
                              85% Full
                            </Badge>
                          </Box>
                        </Card.Section>

                        {/* Event Content */}
                        <Stack gap="md" mt="md">
                          <Group justify="space-between" align="flex-start">
                            <div style={{ flex: 1 }}>
                              <Title order={4} lineClamp={2} mb="xs">
                                Advanced Suspension Mastery
                              </Title>
                              <Text size="sm" c="dimmed" lineClamp={2}>
                                Comprehensive workshop covering advanced suspension techniques, safety protocols, and artistic expression.
                              </Text>
                            </div>
                            <Group gap="xs">
                              <Text fw="bold" c="yellow">★ 4.9</Text>
                            </Group>
                          </Group>

                          {/* Event Timeline */}
                          <Timeline active={1} bulletSize={24} lineWidth={2}>
                            <Timeline.Item bullet={<IconCalendar size={12} />} title="Date & Time">
                              <Text size="sm" c="dimmed">Sat, Dec 14 • 2:00 PM - 5:00 PM</Text>
                            </Timeline.Item>
                            <Timeline.Item bullet={<IconMapPin size={12} />} title="Location">
                              <Text size="sm" c="dimmed">Salem Arts Center</Text>
                            </Timeline.Item>
                          </Timeline>

                          {/* Instructor Info */}
                          <Group gap="sm">
                            <Avatar src={null} radius="xl" color="witchcity">SM</Avatar>
                            <div>
                              <Text fw={500} size="sm">Sarah Mitchell</Text>
                              <Text size="xs" c="dimmed">Master Instructor • 15 years experience</Text>
                            </div>
                          </Group>

                          {/* Capacity with Progress */}
                          <div>
                            <Group justify="space-between" mb="xs">
                              <Text size="sm" fw={500}>Capacity</Text>
                              <Text size="sm" c="dimmed">10 of 12 spots</Text>
                            </Group>
                            <Progress 
                              value={83} 
                              color="witchcity" 
                              size="lg"
                              radius="xl"
                              animate
                            />
                          </div>

                          {/* Price and Registration */}
                          <Group justify="space-between" align="center">
                            <div>
                              <Group gap="xs" align="baseline">
                                <Text fw={700} size="xl" c="witchcity">$85</Text>
                                <Text size="xs" c="dimmed" td="line-through">$120</Text>
                              </Group>
                              <Text size="xs" c="dimmed">Member discount applied</Text>
                            </div>
                            <Button 
                              variant="gradient"
                              gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}
                              size="md"
                              rightSection={<IconArrowRight size={16} />}
                              onClick={() => {
                                notifications.show({
                                  title: 'Registration Started',
                                  message: 'Redirecting to secure checkout...',
                                  color: 'green'
                                });
                              }}
                            >
                              Register
                            </Button>
                          </Group>

                          {/* Tags and Requirements */}
                          <Group gap="xs">
                            <Badge variant="light" color="blue" size="xs">Intermediate</Badge>
                            <Badge variant="light" color="orange" size="xs">Vetted Only</Badge>
                            <Badge variant="light" color="green" size="xs">Safety Cert Required</Badge>
                          </Group>
                        </Stack>
                      </Card>
                    </Grid.Col>
                  ))}
                </Grid>
              )}

              {/* DataTable View */}
              {viewMode === 'table' && (
                <Card withBorder radius="lg" p="lg">
                  <DataTable
                    records={[
                      {
                        id: 1,
                        title: 'Advanced Suspension Mastery',
                        instructor: 'Sarah Mitchell',
                        date: '2024-12-14',
                        time: '2:00 PM - 5:00 PM',
                        location: 'Salem Arts Center',
                        capacity: '10/12',
                        price: '$85',
                        level: 'Intermediate',
                        rating: 4.9
                      },
                      // More events...
                    ]}
                    columns={[
                      {
                        accessor: 'title',
                        title: 'Event',
                        render: ({ title }) => (
                          <Group gap="sm">
                            <Avatar size="sm" radius="md">E</Avatar>
                            <div>
                              <Text fw={500}>{title}</Text>
                              <Text size="xs" c="dimmed">Workshop</Text>
                            </div>
                          </Group>
                        )
                      },
                      { accessor: 'instructor', title: 'Instructor' },
                      { accessor: 'date', title: 'Date' },
                      { accessor: 'location', title: 'Location' },
                      {
                        accessor: 'capacity',
                        title: 'Capacity',
                        render: ({ capacity }) => (
                          <Badge variant="light" color="green">{capacity}</Badge>
                        )
                      },
                      { accessor: 'price', title: 'Price' },
                      {
                        accessor: 'rating',
                        title: 'Rating',
                        render: ({ rating }) => (
                          <Group gap="xs">
                            <IconStar size={14} color="orange" />
                            <Text size="sm">{rating}</Text>
                          </Group>
                        )
                      },
                      {
                        accessor: 'actions',
                        title: 'Actions',
                        render: () => (
                          <Group gap="xs">
                            <ActionIcon variant="subtle" color="blue">
                              <IconEye size={16} />
                            </ActionIcon>
                            <ActionIcon variant="subtle" color="red">
                              <IconHeart size={16} />
                            </ActionIcon>
                            <ActionIcon variant="subtle" color="green">
                              <IconPlus size={16} />
                            </ActionIcon>
                          </Group>
                        )
                      }
                    ]}
                    highlightOnHover
                    verticalSpacing="md"
                    striped
                    withTableBorder
                    withColumnBorders
                  />
                </Card>
              )}

              {/* Load More with Skeleton Loading */}
              <Group justify="center" mt="xl">
                {loading ? (
                  <Stack gap="md" style={{ width: '100%' }}>
                    <Skeleton height={200} radius="md" />
                    <Skeleton height={200} radius="md" />
                    <Skeleton height={200} radius="md" />
                  </Stack>
                ) : (
                  <Button 
                    variant="outline" 
                    color="witchcity" 
                    size="lg"
                    leftSection={<IconPlus size={18} />}
                    onClick={() => {
                      setLoading(true);
                      setTimeout(() => setLoading(false), 2000);
                    }}
                  >
                    Load More Events
                  </Button>
                )}
              </Group>
            </Grid.Col>
          </Grid>
        </Tabs.Panel>

        <Tabs.Panel value="calendar" pt="lg">
          <Card withBorder radius="lg" p="xl">
            <Text size="lg" fw={500} mb="md">Calendar View</Text>
            <Text c="dimmed">Interactive calendar with event scheduling coming soon...</Text>
          </Card>
        </Tabs.Panel>

        <Tabs.Panel value="analytics" pt="lg">
          <Grid>
            <Grid.Col span={12}>
              <Card withBorder radius="lg" p="lg">
                <Title order={3} mb="lg">Event Popularity Trends</Title>
                <AreaChart
                  h={300}
                  data={[
                    { month: 'Jan', workshops: 45, performances: 12, social: 8 },
                    { month: 'Feb', workshops: 52, performances: 18, social: 12 },
                    { month: 'Mar', workshops: 48, performances: 15, social: 10 },
                    { month: 'Apr', workshops: 61, performances: 22, social: 16 },
                    { month: 'May', workshops: 55, performances: 19, social: 14 },
                    { month: 'Jun', workshops: 67, performances: 28, social: 20 }
                  ]}
                  dataKey="month"
                  series={[
                    { name: 'workshops', color: 'witchcity.6' },
                    { name: 'performances', color: 'blue.6' },
                    { name: 'social', color: 'green.6' }
                  ]}
                />
              </Card>
            </Grid.Col>
          </Grid>
        </Tabs.Panel>

        <Tabs.Panel value="saved" pt="lg">
          <Card withBorder radius="lg" p="xl">
            <Text size="lg" fw={500} mb="md">Saved Events</Text>
            <Text c="dimmed">Your bookmarked events will appear here...</Text>
          </Card>
        </Tabs.Panel>
      </Tabs>

      {/* Advanced Filters Drawer */}
      <Drawer
        opened={filtersOpened}
        onClose={closeFilters}
        title="Advanced Filters"
        position="right"
        size="md"
      >
        <Stack gap="lg">
          <Text>Advanced filtering options would go here...</Text>
        </Stack>
      </Drawer>

      <style>{`
        .event-overlay {
          opacity: 0;
        }
        
        .mantine-Card-root:hover .event-overlay {
          opacity: 1;
        }
      `}</style>
    </Container>
  );
};
```

### Unique Features
- **DataTable Integration**: Sortable, filterable event management table
- **Advanced Analytics**: Real-time charts and community metrics
- **Spotlight Search**: Global search with Ctrl+K activation
- **Multi-Select Filtering**: Complex filtering with MultiSelect components
- **Interactive Timeline**: Event detail progression
- **Rich Notifications**: Toast notifications for user actions
- **Skeleton Loading**: Professional loading states
- **Tab-Based Navigation**: Organized content sections
- **Progress Tracking**: Visual capacity and rating indicators

---

## Variation 5: Template-Inspired Ultra-Modern Events (Revolutionary)
**Edginess Level**: 5/5 | **Alignment**: Homepage Variation 5

### Design Philosophy
Analytics dashboard-inspired design with professional-grade UX patterns, comprehensive data visualization, and enterprise-level event management that transforms the community platform into a sophisticated business tool.

### Visual Characteristics
- **Dashboard Aesthetic**: Multi-panel layout with integrated analytics
- **Professional Polish**: Enterprise SaaS-level design quality
- **Data Integration**: Real-time community metrics and predictive insights
- **Advanced UX**: Professional workflow patterns and user analytics

### Mantine v7 Implementation
```typescript
import { 
  Grid, 
  Card, 
  Image, 
  Text, 
  Badge, 
  Button, 
  Group, 
  Stack, 
  Container,
  Title,
  TextInput,
  ActionIcon,
  Box,
  Avatar,
  AppShell,
  NavLink,
  Navbar,
  Header,
  Aside,
  SimpleGrid,
  RingProgress,
  AreaChart,
  BarChart,
  DonutChart,
  ThemeIcon,
  Paper,
  Spotlight,
  Tabs,
  Timeline,
  Stats,
  Divider
} from '@mantine/core';
import { 
  IconDashboard,
  IconCalendar,
  IconUsers,
  IconChartBar,
  IconSettings,
  IconBell,
  IconTrendingUp,
  IconTrendingDown,
  IconArrowUp,
  IconMapPin,
  IconClock,
  IconMoneybag,
  IconStar,
  IconEye
} from '@tabler/icons-react';

const TemplateInspiredEvents = () => {
  return (
    <AppShell
      header={{ height: 70 }}
      navbar={{ width: 280, breakpoint: 'md' }}
      aside={{ width: 350, breakpoint: 'lg' }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group>
            <Title order={3} c="witchcity">WitchCityRope</Title>
            <Badge variant="light" color="green">Live Dashboard</Badge>
          </Group>
          <Group gap="sm">
            <ActionIcon variant="light" color="blue">
              <IconBell size={18} />
            </ActionIcon>
            <Avatar radius="xl" size="sm">A</Avatar>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="md">
        <Stack gap={0}>
          <Text fw={500} size="sm" c="dimmed" mb="sm">NAVIGATION</Text>
          <NavLink
            href="#"
            label="Dashboard"
            leftSection={<IconDashboard size="1rem" />}
          />
          <NavLink
            href="#"
            label="Events"
            leftSection={<IconCalendar size="1rem" />}
            active
          />
          <NavLink
            href="#"
            label="Members"
            leftSection={<IconUsers size="1rem" />}
          />
          <NavLink
            href="#"
            label="Analytics"
            leftSection={<IconChartBar size="1rem" />}
          />
          
          <Text fw={500} size="sm" c="dimmed" mb="sm" mt="xl">MANAGEMENT</Text>
          <NavLink
            href="#"
            label="Event Creation"
            leftSection={<IconCalendar size="1rem" />}
          />
          <NavLink
            href="#"
            label="Instructor Hub"
            leftSection={<IconUsers size="1rem" />}
          />
          <NavLink
            href="#"
            label="Reports"
            leftSection={<IconChartBar size="1rem" />}
          />
          <NavLink
            href="#"
            label="Settings"
            leftSection={<IconSettings size="1rem" />}
          />
        </Stack>
      </AppShell.Navbar>

      <AppShell.Main>
        {/* Dashboard Header */}
        <Group justify="space-between" mb="xl">
          <div>
            <Title order={1} size="2rem" fw="bold">Events Dashboard</Title>
            <Text c="dimmed">Comprehensive event management and analytics</Text>
          </div>
          <Group gap="sm">
            <Button variant="light" leftSection={<IconCalendar size={16} />}>
              New Event
            </Button>
            <Button variant="gradient" gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}>
              Export Data
            </Button>
          </Group>
        </Group>

        {/* Key Metrics Grid */}
        <SimpleGrid cols={{ base: 1, sm: 2, md: 4 }} mb="xl">
          <Paper withBorder radius="lg" p="lg">
            <Group justify="space-between">
              <div>
                <Text size="xs" tt="uppercase" fw={700} c="dimmed">Total Events</Text>
                <Group align="flex-end" gap="xs" mt="xs">
                  <Text size="xl" fw="bold">156</Text>
                  <Group gap={4} align="center">
                    <IconTrendingUp size={16} color="green" />
                    <Text size="sm" c="green">+12%</Text>
                  </Group>
                </Group>
              </div>
              <ThemeIcon color="witchcity" variant="light" size="xl" radius="md">
                <IconCalendar size={22} />
              </ThemeIcon>
            </Group>
          </Paper>

          <Paper withBorder radius="lg" p="lg">
            <Group justify="space-between">
              <div>
                <Text size="xs" tt="uppercase" fw={700} c="dimmed">Total Registrations</Text>
                <Group align="flex-end" gap="xs" mt="xs">
                  <Text size="xl" fw="bold">2,847</Text>
                  <Group gap={4} align="center">
                    <IconTrendingUp size={16} color="green" />
                    <Text size="sm" c="green">+28%</Text>
                  </Group>
                </Group>
              </div>
              <ThemeIcon color="blue" variant="light" size="xl" radius="md">
                <IconUsers size={22} />
              </ThemeIcon>
            </Group>
          </Paper>

          <Paper withBorder radius="lg" p="lg">
            <Group justify="space-between">
              <div>
                <Text size="xs" tt="uppercase" fw={700} c="dimmed">Revenue</Text>
                <Group align="flex-end" gap="xs" mt="xs">
                  <Text size="xl" fw="bold">$24,589</Text>
                  <Group gap={4} align="center">
                    <IconTrendingUp size={16} color="green" />
                    <Text size="sm" c="green">+15%</Text>
                  </Group>
                </Group>
              </div>
              <ThemeIcon color="green" variant="light" size="xl" radius="md">
                <IconMoneybag size={22} />
              </ThemeIcon>
            </Group>
          </Paper>

          <Paper withBorder radius="lg" p="lg">
            <Group justify="space-between">
              <div>
                <Text size="xs" tt="uppercase" fw={700} c="dimmed">Avg Rating</Text>
                <Group align="flex-end" gap="xs" mt="xs">
                  <Text size="xl" fw="bold">4.8</Text>
                  <Group gap={4} align="center">
                    <IconStar size={16} color="orange" />
                    <Text size="sm" c="orange">+0.2</Text>
                  </Group>
                </Group>
              </div>
              <ThemeIcon color="orange" variant="light" size="xl" radius="md">
                <IconStar size={22} />
              </ThemeIcon>
            </Group>
          </Paper>
        </SimpleGrid>

        {/* Analytics Charts */}
        <Grid mb="xl">
          <Grid.Col span={{ base: 12, md: 8 }}>
            <Card withBorder radius="lg" p="lg">
              <Group justify="space-between" mb="lg">
                <Title order={3}>Event Registration Trends</Title>
                <Badge variant="light" color="blue">Last 6 months</Badge>
              </Group>
              <AreaChart
                h={300}
                data={[
                  { month: 'Jul', workshops: 45, performances: 12, social: 8, total: 65 },
                  { month: 'Aug', workshops: 52, performances: 18, social: 12, total: 82 },
                  { month: 'Sep', workshops: 48, performances: 15, social: 10, total: 73 },
                  { month: 'Oct', workshops: 61, performances: 22, social: 16, total: 99 },
                  { month: 'Nov', workshops: 55, performances: 19, social: 14, total: 88 },
                  { month: 'Dec', workshops: 67, performances: 28, social: 20, total: 115 }
                ]}
                dataKey="month"
                series={[
                  { name: 'total', color: 'witchcity.6', label: 'Total Registrations' },
                  { name: 'workshops', color: 'blue.6', label: 'Workshops' },
                  { name: 'performances', color: 'green.6', label: 'Performances' },
                  { name: 'social', color: 'orange.6', label: 'Social Events' }
                ]}
                withLegend
                curveType="bump"
                connectNulls={false}
              />
            </Card>
          </Grid.Col>

          <Grid.Col span={{ base: 12, md: 4 }}>
            <Card withBorder radius="lg" p="lg" h="100%">
              <Title order={3} mb="lg">Event Type Distribution</Title>
              <DonutChart
                size={200}
                thickness={30}
                data={[
                  { name: 'Workshops', value: 62, color: 'witchcity.6' },
                  { name: 'Performances', value: 18, color: 'blue.6' },
                  { name: 'Social', value: 12, color: 'green.6' },
                  { name: 'Educational', value: 8, color: 'orange.6' }
                ]}
                withLabels
                withTooltip
              />
            </Card>
          </Grid.Col>
        </Grid>

        {/* Event Management Table */}
        <Card withBorder radius="lg" p="lg" mb="xl">
          <Group justify="space-between" mb="lg">
            <Title order={3}>Upcoming Events</Title>
            <Group gap="sm">
              <TextInput placeholder="Search events..." leftSection={<IconEye size={16} />} />
              <Button variant="light" size="sm">Filter</Button>
            </Group>
          </Group>

          <Box style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid #E9ECEF' }}>
                  <th style={{ textAlign: 'left', padding: '12px', fontWeight: 600 }}>Event</th>
                  <th style={{ textAlign: 'left', padding: '12px', fontWeight: 600 }}>Date & Time</th>
                  <th style={{ textAlign: 'left', padding: '12px', fontWeight: 600 }}>Instructor</th>
                  <th style={{ textAlign: 'left', padding: '12px', fontWeight: 600 }}>Capacity</th>
                  <th style={{ textAlign: 'left', padding: '12px', fontWeight: 600 }}>Revenue</th>
                  <th style={{ textAlign: 'left', padding: '12px', fontWeight: 600 }}>Status</th>
                  <th style={{ textAlign: 'left', padding: '12px', fontWeight: 600 }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {[
                  {
                    title: 'Advanced Suspension Mastery',
                    date: 'Dec 14, 2:00 PM',
                    instructor: 'Sarah Mitchell',
                    capacity: '10/12',
                    revenue: '$850',
                    status: 'Active',
                    rating: 4.9
                  },
                  {
                    title: 'Rope Bondage Performance Art',
                    date: 'Dec 16, 7:00 PM',
                    instructor: 'Alex Chen',
                    capacity: '25/30',
                    revenue: '$1,250',
                    status: 'Selling Fast',
                    rating: 4.8
                  },
                  {
                    title: 'Beginner Safety Workshop',
                    date: 'Dec 18, 1:00 PM',
                    instructor: 'Jamie Rodriguez',
                    capacity: '8/15',
                    revenue: '$400',
                    status: 'Available',
                    rating: 4.7
                  }
                ].map((event, index) => (
                  <tr key={index} style={{ borderBottom: '1px solid #F1F3F4' }}>
                    <td style={{ padding: '16px' }}>
                      <Group gap="sm">
                        <Avatar size="md" radius="md" color="witchcity">E</Avatar>
                        <div>
                          <Text fw={500}>{event.title}</Text>
                          <Group gap={4} align="center">
                            <IconStar size={12} color="orange" />
                            <Text size="sm" c="dimmed">{event.rating}</Text>
                          </Group>
                        </div>
                      </Group>
                    </td>
                    <td style={{ padding: '16px' }}>
                      <Group gap={4} align="center">
                        <IconClock size={14} />
                        <Text size="sm">{event.date}</Text>
                      </Group>
                    </td>
                    <td style={{ padding: '16px' }}>
                      <Text size="sm">{event.instructor}</Text>
                    </td>
                    <td style={{ padding: '16px' }}>
                      <Group gap="xs">
                        <Text size="sm">{event.capacity}</Text>
                        <Progress 
                          value={parseInt(event.capacity.split('/')[0]) / parseInt(event.capacity.split('/')[1]) * 100} 
                          size="xs" 
                          style={{ width: 60 }}
                          color="witchcity"
                        />
                      </Group>
                    </td>
                    <td style={{ padding: '16px' }}>
                      <Text size="sm" fw={500} c="green">{event.revenue}</Text>
                    </td>
                    <td style={{ padding: '16px' }}>
                      <Badge 
                        variant="light" 
                        color={event.status === 'Active' ? 'green' : event.status === 'Selling Fast' ? 'orange' : 'blue'}
                        size="sm"
                      >
                        {event.status}
                      </Badge>
                    </td>
                    <td style={{ padding: '16px' }}>
                      <Group gap="xs">
                        <ActionIcon variant="subtle" color="blue" size="sm">
                          <IconEye size={14} />
                        </ActionIcon>
                        <ActionIcon variant="subtle" color="witchcity" size="sm">
                          <IconSettings size={14} />
                        </ActionIcon>
                      </Group>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Box>
        </Card>
      </AppShell.Main>

      <AppShell.Aside p="md">
        <Stack gap="lg">
          {/* Quick Stats */}
          <Card withBorder radius="lg" p="lg">
            <Title order={4} mb="md">Live Activity</Title>
            <Stack gap="md">
              <Group justify="space-between">
                <Text size="sm">Active Users</Text>
                <Badge variant="light" color="green">47</Badge>
              </Group>
              <Group justify="space-between">
                <Text size="sm">New Registrations</Text>
                <Badge variant="light" color="blue">12</Badge>
              </Group>
              <Group justify="space-between">
                <Text size="sm">Revenue Today</Text>
                <Badge variant="light" color="orange">$1,250</Badge>
              </Group>
            </Stack>
          </Card>

          {/* Popular Events */}
          <Card withBorder radius="lg" p="lg">
            <Title order={4} mb="md">Trending Events</Title>
            <Stack gap="md">
              {[
                { title: 'Suspension Mastery', views: 247 },
                { title: 'Safety Workshop', views: 189 },
                { title: 'Performance Art', views: 156 }
              ].map((event, index) => (
                <Group key={index} justify="space-between">
                  <div>
                    <Text size="sm" fw={500}>{event.title}</Text>
                    <Text size="xs" c="dimmed">{event.views} views</Text>
                  </div>
                  <IconTrendingUp size={16} color="green" />
                </Group>
              ))}
            </Stack>
          </Card>

          {/* Recent Activity Timeline */}
          <Card withBorder radius="lg" p="lg">
            <Title order={4} mb="md">Recent Activity</Title>
            <Timeline active={-1} bulletSize={20} lineWidth={1}>
              <Timeline.Item bullet={<IconUsers size={12} />} title="New Registration">
                <Text size="xs" c="dimmed">Sarah joined Suspension Mastery</Text>
                <Text size="xs" c="dimmed">2 minutes ago</Text>
              </Timeline.Item>
              <Timeline.Item bullet={<IconCalendar size={12} />} title="Event Updated">
                <Text size="xs" c="dimmed">Safety Workshop capacity increased</Text>
                <Text size="xs" c="dimmed">15 minutes ago</Text>
              </Timeline.Item>
              <Timeline.Item bullet={<IconMoneybag size={12} />} title="Payment Received">
                <Text size="xs" c="dimmed">$85 for Advanced Suspension</Text>
                <Text size="xs" c="dimmed">1 hour ago</Text>
              </Timeline.Item>
            </Timeline>
          </Card>

          {/* Performance Metrics */}
          <Card withBorder radius="lg" p="lg">
            <Title order={4} mb="md">Performance</Title>
            <Stack gap="lg">
              <div>
                <Group justify="space-between" mb="xs">
                  <Text size="sm">Event Fill Rate</Text>
                  <Text size="sm" fw={500}>85%</Text>
                </Group>
                <Progress value={85} color="witchcity" size="sm" radius="xl" />
              </div>
              
              <div>
                <Group justify="space-between" mb="xs">
                  <Text size="sm">Member Satisfaction</Text>
                  <Text size="sm" fw={500}>96%</Text>
                </Group>
                <Progress value={96} color="green" size="sm" radius="xl" />
              </div>
              
              <div>
                <Group justify="space-between" mb="xs">
                  <Text size="sm">Revenue Target</Text>
                  <Text size="sm" fw={500}>73%</Text>
                </Group>
                <Progress value={73} color="blue" size="sm" radius="xl" />
              </div>
            </Stack>
          </Card>
        </Stack>
      </AppShell.Aside>
    </AppShell>
  );
};
```

### Unique Features
- **AppShell Architecture**: Full dashboard layout with sidebar navigation
- **Live Analytics**: Real-time community metrics and performance indicators
- **Professional Data Tables**: Comprehensive event management interface
- **Multi-Panel Dashboard**: Integrated analytics, activity feeds, and performance metrics
- **Revenue Tracking**: Financial analytics and business intelligence
- **Activity Timeline**: Real-time community activity monitoring
- **Performance Metrics**: KPI tracking with visual progress indicators
- **Enterprise UX**: Professional workflow patterns and user analytics

---

## Implementation Guidelines

### Responsive Design Strategy

All variations implement mobile-first responsive design with consistent breakpoints:

```typescript
const responsiveBreakpoints = {
  xs: '0px - 575px',    // Mobile
  sm: '576px - 767px',  // Large mobile
  md: '768px - 991px',  // Tablet
  lg: '992px - 1199px', // Desktop
  xl: '1200px+'         // Large desktop
};
```

### Common Component Patterns

#### Event Card Structure
```typescript
const EventCard = {
  header: {
    badge: 'Event type indicator',
    quickActions: 'Heart, share, bookmark'
  },
  image: {
    eventPhoto: 'High-quality event imagery',
    overlay: 'Hover effects and actions'
  },
  content: {
    title: 'Clear, descriptive event title',
    description: 'Concise event description',
    details: 'Date, time, location, instructor',
    capacity: 'Visual progress indicators',
    pricing: 'Clear pricing with member discounts'
  },
  actions: {
    primary: 'Register/RSVP button',
    secondary: 'Save, share, more info'
  },
  metadata: {
    tags: 'Skill level, access requirements',
    rating: 'User ratings and reviews'
  }
};
```

### Accessibility Standards

All variations maintain WCAG 2.1 AA compliance:

- **Keyboard Navigation**: Full functionality without mouse
- **Screen Reader Support**: Proper ARIA labels and descriptions
- **Color Contrast**: Minimum 4.5:1 ratio maintained
- **Focus Management**: Clear focus indicators and logical tab order
- **Motion Respect**: All animations respect `prefers-reduced-motion`

### Performance Optimization

```typescript
const performancePatterns = {
  imageLoading: 'Lazy loading with placeholder skeletons',
  dataFetching: 'Paginated loading with infinite scroll',
  animations: 'GPU-accelerated transforms with fallbacks',
  filtering: 'Debounced search with client-side caching',
  stateManagement: 'Optimized React state with useCallback'
};
```

## Stakeholder Review Process

### Selection Criteria

1. **Visual Impact**: Alignment with community aesthetic and brand
2. **User Experience**: Event discovery and registration flow efficiency
3. **Implementation Complexity**: Development timeline and resource requirements
4. **Scalability**: Future feature expansion and community growth support
5. **Mobile Experience**: Touch-friendly interface and mobile optimization

### Implementation Timeline

| Variation | Timeline | Complexity | Key Features |
|-----------|----------|------------|--------------|
| Enhanced Current | 1-2 weeks | Low | Clean cards, basic filtering |
| Dark Theme Focus | 2-3 weeks | Medium | Neon effects, terminal aesthetic |
| Geometric Modern | 3-4 weeks | High | Clip-path styling, angular design |
| Advanced Mantine | 4-6 weeks | High | DataTable, analytics, rich components |
| Template-Inspired | 6-8 weeks | Very High | Full dashboard, enterprise features |

### Next Steps

1. **Stakeholder Selection**: Choose preferred variation or hybrid approach
2. **Feature Prioritization**: Identify must-have vs nice-to-have features
3. **Technical Planning**: Sprint planning and resource allocation
4. **Design Refinement**: Incorporate feedback and finalize specifications
5. **Development Handoff**: Complete component mapping and implementation guides

Each events page variation provides a comprehensive solution for community event discovery and management while maintaining aesthetic coherence with its corresponding homepage design. The progressive complexity allows stakeholders to select their preferred balance of visual impact, functionality, and implementation timeline.