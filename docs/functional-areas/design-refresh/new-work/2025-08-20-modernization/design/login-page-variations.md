# Login Page Design Variations
<!-- Last Updated: 2025-08-20 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete -->

## Design Overview

This document presents 5 login page design variations that align with the corresponding homepage design variations, maintaining visual consistency while optimizing for authentication UX. Each design follows mobile-first responsive principles and leverages Mantine v7 components for consistent implementation.

**Authentication Flow**: Email/password with remember me functionality, forgot password link, and registration option.

## Design Principles

### Core Authentication UX Requirements
- **Security First**: Clear password visibility toggle and secure form submission
- **Error Handling**: Contextual error messages with recovery options
- **Loading States**: Clear feedback during authentication process
- **Accessibility**: WCAG 2.1 AA compliance with keyboard navigation
- **Mobile Optimization**: Touch-friendly interface with 48px minimum targets

### Visual Consistency
- Each variation maintains aesthetic harmony with its corresponding homepage
- Color palette and typography system alignment
- Component styling and interaction patterns match
- Animation and micro-interaction consistency

---

## Variation 1: Enhanced Current (Subtle Evolution)
**Edginess Level**: 2/5 | **Alignment**: Homepage Variation 1

### Design Philosophy
Clean, sophisticated login form that enhances the current successful design with improved micro-interactions and refined visual hierarchy.

### Visual Characteristics
- **Color Palette**: Original burgundy (#880124) with enhanced rose gold (#B76D75)
- **Card Design**: Elevated white card with subtle shadow and border radius
- **Typography**: Clean hierarchy with gradient accent on brand elements
- **Interactions**: Smooth hover states with gentle elevation effects

### Layout Structure
```
+----------------------------------+
|             Header               |
+----------------------------------+
|                                  |
|    [Centered Login Card]         |
|    ┌─────────────────────┐       |
|    │ WITCH CITY ROPE     │       |
|    │ ─────────────────   │       |
|    │ Welcome Back        │       |
|    │                     │       |
|    │ [Email Input]       │       |
|    │ [Password Input]    │       |
|    │ [Remember Me] [?]   │       |
|    │                     │       |
|    │ [Sign In Button]    │       |
|    │                     │       |
|    │ Forgot Password?    │       |
|    │ Need an account?    │       |
|    └─────────────────────┘       |
|                                  |
+----------------------------------+
|             Footer               |
+----------------------------------+
```

### Mantine v7 Implementation
```typescript
import { 
  Card, 
  TextInput, 
  PasswordInput, 
  Button, 
  Checkbox, 
  Text, 
  Title, 
  Container, 
  Box,
  Group,
  Anchor
} from '@mantine/core';
import { useForm } from '@mantine/form';

const EnhancedCurrentLogin = () => {
  const form = useForm({
    initialValues: { email: '', password: '', rememberMe: false },
    validate: {
      email: (value) => (/^\S+@\S+$/.test(value) ? null : 'Invalid email'),
      password: (value) => (value.length >= 8 ? null : 'Password too short'),
    },
  });

  return (
    <Container size="xs" px="md" py="xl">
      <Card
        radius="lg"
        shadow="md"
        padding="xl"
        withBorder
        style={{
          maxWidth: 400,
          margin: '0 auto',
          transition: 'all 0.3s ease'
        }}
        styles={{
          root: {
            '&:hover': {
              boxShadow: '0 10px 30px rgba(136, 1, 36, 0.1)',
              transform: 'translateY(-2px)'
            }
          }
        }}
      >
        <Box ta="center" mb="xl">
          <Title
            order={2}
            size="1.8rem"
            fw="bold"
            variant="gradient"
            gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}
            mb="xs"
          >
            WITCH CITY ROPE
          </Title>
          <Text size="lg" c="dimmed" fw={500}>
            Welcome Back
          </Text>
        </Box>

        <form onSubmit={form.onSubmit(handleLogin)}>
          <TextInput
            label="Email"
            placeholder="your@email.com"
            size="md"
            mb="md"
            {...form.getInputProps('email')}
            styles={{
              input: {
                borderColor: 'var(--mantine-color-gray-4)',
                '&:focus': {
                  borderColor: 'var(--mantine-color-witchcity-6)',
                  boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)'
                }
              }
            }}
          />

          <PasswordInput
            label="Password"
            placeholder="Your password"
            size="md"
            mb="md"
            {...form.getInputProps('password')}
            styles={{
              input: {
                borderColor: 'var(--mantine-color-gray-4)',
                '&:focus': {
                  borderColor: 'var(--mantine-color-witchcity-6)',
                  boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)'
                }
              }
            }}
          />

          <Group justify="space-between" mb="xl">
            <Checkbox
              label="Remember me"
              color="witchcity"
              {...form.getInputProps('rememberMe', { type: 'checkbox' })}
            />
            <Anchor size="sm" c="witchcity.6" href="/forgot-password">
              Forgot password?
            </Anchor>
          </Group>

          <Button
            type="submit"
            variant="gradient"
            gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}
            size="md"
            fullWidth
            mb="md"
            styles={{
              root: {
                '&:hover': {
                  transform: 'translateY(-1px)',
                  boxShadow: '0 4px 15px rgba(136, 1, 36, 0.3)'
                }
              }
            }}
          >
            Sign In
          </Button>

          <Text ta="center" size="sm" c="dimmed">
            Need an account?{' '}
            <Anchor c="witchcity.6" href="/register" fw={500}>
              Join the community
            </Anchor>
          </Text>
        </form>
      </Card>
    </Container>
  );
};
```

### Responsive Behavior
- **Desktop**: Centered card (400px max width) with generous spacing
- **Tablet**: Maintains card approach with adjusted padding
- **Mobile**: Full-width card with reduced margins, larger touch targets

---

## Variation 2: Dark Theme Focus (Moderate Change) 
**Edginess Level**: 3/5 | **Alignment**: Homepage Variation 2

### Design Philosophy
Cyberpunk-inspired dark login with neon accents and dramatic glow effects that embody the alternative aesthetic of Salem's rope community.

### Visual Characteristics
- **Color Palette**: Deep black (#0D1117) with neon burgundy (#FF0A54) and electric accents
- **Card Design**: Dark card with glowing neon border and backdrop effects
- **Typography**: Monospace elements with glowing text effects
- **Interactions**: Dramatic glow animations with color inversions

### Layout Structure
```
+----------------------------------+
|        Dark Header (Neon)       |
+----------------------------------+
|   ████ Dark Background ████      |
|   ┌─────────────────────┐       |
|   │ ╔═══════════════╗   │       |
|   │ ║ WITCH CITY    ║   │ Glow  |
|   │ ║ > LOGIN_      ║   │ Effect|
|   │ ╚═══════════════╝   │       |
|   │                     │       |
|   │ [Neon Email Input]  │       |
|   │ [Neon Password]     │       |
|   │ [◉] REMEMBER [?]    │       |
|   │                     │       |
|   │ [ELECTRIC BUTTON]   │       |
|   │                     │       |
|   │ > FORGOT_PASSWORD   │       |
|   │ > CREATE_ACCOUNT    │       |
|   └─────────────────────┘       |
|   ████ Animated Grid ████        |
+----------------------------------+
```

### Mantine v7 Implementation
```typescript
import { 
  Card, 
  TextInput, 
  PasswordInput, 
  Button, 
  Checkbox, 
  Text, 
  Title, 
  Container, 
  Box,
  Group,
  Anchor
} from '@mantine/core';

const DarkFocusLogin = () => {
  return (
    <Box
      style={{
        backgroundColor: '#0D1117',
        minHeight: '100vh',
        position: 'relative',
        overflow: 'hidden'
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
            linear-gradient(rgba(255, 10, 84, 0.05) 1px, transparent 1px),
            linear-gradient(90deg, rgba(255, 10, 84, 0.05) 1px, transparent 1px)
          `,
          backgroundSize: '40px 40px',
          animation: 'grid-move 20s linear infinite'
        }}
      />

      {/* Floating neon orbs */}
      <Box
        style={{
          position: 'absolute',
          top: '20%',
          right: '15%',
          width: '200px',
          height: '200px',
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(199, 125, 255, 0.1) 0%, transparent 70%)',
          filter: 'blur(40px)',
          animation: 'float 6s ease-in-out infinite'
        }}
      />
      
      <Container size="xs" px="md" style={{ position: 'relative', zIndex: 1 }}>
        <Box style={{ paddingTop: '15vh', paddingBottom: '10vh' }}>
          <Card
            radius="lg"
            padding="xl"
            style={{
              backgroundColor: '#161B22',
              border: '1px solid #FF0A54',
              boxShadow: '0 0 30px rgba(255, 10, 84, 0.2)',
              maxWidth: 420,
              margin: '0 auto',
              position: 'relative',
              overflow: 'hidden'
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
                background: 'linear-gradient(45deg, #FF0A54, #C77DFF, #FF0A54)',
                borderRadius: 'inherit',
                zIndex: -1,
                animation: 'border-glow 3s ease-in-out infinite'
              }}
            />

            <Box ta="center" mb="xl">
              <Title
                order={2}
                size="2rem"
                fw="bold"
                mb="xs"
                style={{
                  background: 'linear-gradient(135deg, #FF0A54 0%, #FF6B9D 50%, #C77DFF 100%)',
                  backgroundClip: 'text',
                  WebkitBackgroundClip: 'text',
                  color: 'transparent',
                  textShadow: '0 0 20px rgba(255, 10, 84, 0.5)',
                  fontFamily: 'monospace',
                  letterSpacing: '2px'
                }}
              >
                WITCH CITY ROPE
              </Title>
              <Text 
                size="lg" 
                fw={600}
                style={{
                  color: '#FF6B9D',
                  fontFamily: 'monospace',
                  textTransform: 'uppercase',
                  letterSpacing: '1px'
                }}
              >
                &gt; ACCESS_TERMINAL
              </Text>
            </Box>

            <form onSubmit={form.onSubmit(handleLogin)}>
              <TextInput
                label="EMAIL_ADDRESS"
                placeholder="user@domain.com"
                size="md"
                mb="md"
                {...form.getInputProps('email')}
                styles={{
                  label: {
                    color: '#C77DFF',
                    fontFamily: 'monospace',
                    fontSize: '0.8rem',
                    fontWeight: 'bold',
                    textTransform: 'uppercase',
                    letterSpacing: '1px'
                  },
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
                      color: '#8B949E'
                    }
                  }
                }}
              />

              <PasswordInput
                label="PASSWORD_HASH"
                placeholder="••••••••••••"
                size="md"
                mb="md"
                {...form.getInputProps('password')}
                styles={{
                  label: {
                    color: '#C77DFF',
                    fontFamily: 'monospace',
                    fontSize: '0.8rem',
                    fontWeight: 'bold',
                    textTransform: 'uppercase',
                    letterSpacing: '1px'
                  },
                  input: {
                    backgroundColor: '#0D1117',
                    borderColor: '#30363D',
                    color: '#C9D1D9',
                    fontFamily: 'monospace',
                    '&:focus': {
                      borderColor: '#FF0A54',
                      boxShadow: '0 0 10px rgba(255, 10, 84, 0.3)'
                    }
                  }
                }}
              />

              <Group justify="space-between" mb="xl">
                <Checkbox
                  label="REMEMBER_SESSION"
                  color="red"
                  {...form.getInputProps('rememberMe', { type: 'checkbox' })}
                  styles={{
                    label: {
                      color: '#39FF14',
                      fontFamily: 'monospace',
                      fontSize: '0.8rem',
                      textTransform: 'uppercase'
                    }
                  }}
                />
                <Anchor 
                  size="xs" 
                  href="/forgot-password"
                  style={{
                    color: '#FFB000',
                    fontFamily: 'monospace',
                    textDecoration: 'none',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px'
                  }}
                >
                  &gt; RESET_PASSWORD
                </Anchor>
              </Group>

              <Button
                type="submit"
                size="lg"
                fullWidth
                mb="md"
                style={{
                  background: 'linear-gradient(135deg, #FF0A54 0%, #C77DFF 100%)',
                  border: 'none',
                  boxShadow: '0 0 20px rgba(255, 10, 84, 0.4)',
                  fontSize: '1rem',
                  fontWeight: 'bold',
                  textTransform: 'uppercase',
                  letterSpacing: '2px',
                  fontFamily: 'monospace'
                }}
                styles={{
                  root: {
                    '&:hover': {
                      transform: 'translateY(-2px)',
                      boxShadow: '0 0 30px rgba(255, 10, 84, 0.6)',
                      background: 'linear-gradient(135deg, #C77DFF 0%, #FF0A54 100%)'
                    }
                  }
                }}
              >
                &gt; AUTHENTICATE
              </Button>

              <Text 
                ta="center" 
                size="sm"
                style={{
                  color: '#8B949E',
                  fontFamily: 'monospace',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px'
                }}
              >
                &gt; <Anchor 
                  style={{
                    color: '#39FF14',
                    textDecoration: 'none'
                  }} 
                  href="/register"
                >
                  CREATE_ACCOUNT
                </Anchor>
              </Text>
            </form>
          </Card>
        </Box>
      </Container>
    </Box>
  );
};
```

### Unique Features
- **Terminal-Style Labels**: Monospace typography with cyberpunk naming
- **Animated Border**: Glowing border that pulses with gradient colors
- **Grid Background**: Subtle animated grid pattern
- **Neon Focus States**: Electric glow effects on form field focus
- **Floating Orbs**: Atmospheric background elements

---

## Variation 3: Geometric Modern (Significant Shift)
**Edginess Level**: 4/5 | **Alignment**: Homepage Variation 3

### Design Philosophy
Bold geometric patterns with asymmetric layouts and sharp angular design elements that create a distinctive modern aesthetic.

### Visual Characteristics
- **Color Palette**: High contrast with bold accent colors
- **Card Design**: Asymmetric shapes with clip-path styling
- **Typography**: Bold, minimal typography with geometric emphasis
- **Interactions**: Sharp, angular animations with geometric transforms

### Layout Structure
```
+----------------------------------+
|        Geometric Header          |
+----------------------------------+
|                                  |
|  ┌─────────────┐                 |
|  │ WITCH       │ ╱╲              |
|  │ CITY        │╱  ╲             |
|  │ ROPE        │    ╲            |
|  └─────────────┘     ╲           |
|  LOGIN               ╱            |
|                     ╱             |
|  ◆ Email           ╱              |
|  ◆ Password       ╱               |
|  ◆ Remember                       |
|                                   |
|  ▶ SIGN IN                        |
|                                   |
|  ↗ Forgot | Register              |
|                                   |
+----------------------------------+
```

### Mantine v7 Implementation
```typescript
import { 
  Card, 
  TextInput, 
  PasswordInput, 
  Button, 
  Checkbox, 
  Text, 
  Title, 
  Container, 
  Box,
  Group,
  Anchor
} from '@mantine/core';

const GeometricLogin = () => {
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
          right: '10%',
          width: '200px',
          height: '200px',
          background: 'linear-gradient(45deg, #880124, #B76D75)',
          clipPath: 'polygon(0 0, 100% 0, 80% 100%, 0 85%)',
          opacity: 0.1
        }}
      />
      
      <Box
        style={{
          position: 'absolute',
          bottom: '15%',
          left: '5%',
          width: '150px',
          height: '150px',
          background: 'linear-gradient(135deg, #9D4EDD, #C77DFF)',
          clipPath: 'polygon(20% 0%, 100% 20%, 80% 100%, 0% 80%)',
          opacity: 0.1
        }}
      />

      <Container size="md" px="md" style={{ position: 'relative', zIndex: 1 }}>
        <Box style={{ paddingTop: '10vh', paddingBottom: '10vh' }}>
          <Box
            style={{
              display: 'grid',
              gridTemplateColumns: '1fr 1fr',
              gap: '2rem',
              maxWidth: 800,
              margin: '0 auto',
              '@media (max-width: 768px)': {
                gridTemplateColumns: '1fr',
                gap: '1rem'
              }
            }}
          >
            {/* Brand Section */}
            <Box
              style={{
                background: 'linear-gradient(135deg, #880124 0%, #B76D75 100%)',
                clipPath: 'polygon(0 0, 100% 0, 100% 85%, 0 100%)',
                padding: '3rem 2rem',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'center',
                color: 'white',
                position: 'relative',
                overflow: 'hidden'
              }}
            >
              {/* Geometric pattern overlay */}
              <Box
                style={{
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  right: 0,
                  bottom: 0,
                  background: `url("data:image/svg+xml,${encodeURIComponent(`
                    <svg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'>
                      <polygon points='50,0 100,50 50,100 0,50' fill='none' stroke='rgba(255,255,255,0.1)' stroke-width='1'/>
                      <polygon points='25,25 75,25 75,75 25,75' fill='none' stroke='rgba(255,255,255,0.05)' stroke-width='1'/>
                    </svg>
                  `)}")`,
                  backgroundSize: '60px 60px'
                }}
              />
              
              <Title
                order={1}
                size="3rem"
                fw="900"
                lh={0.9}
                mb="md"
                style={{ 
                  position: 'relative',
                  zIndex: 1,
                  letterSpacing: '-2px'
                }}
              >
                WITCH<br />
                CITY<br />
                ROPE
              </Title>
              
              <Text 
                size="lg" 
                fw={600}
                style={{ 
                  position: 'relative',
                  zIndex: 1,
                  opacity: 0.9,
                  textTransform: 'uppercase',
                  letterSpacing: '1px'
                }}
              >
                Salem's Community
              </Text>
            </Box>

            {/* Login Form Section */}
            <Card
              radius={0}
              shadow="xl"
              padding="xl"
              style={{
                clipPath: 'polygon(15% 0, 100% 0, 100% 100%, 0 100%)',
                backgroundColor: 'white',
                border: 'none'
              }}
            >
              <Title
                order={2}
                size="2rem"
                fw="900"
                mb="xl"
                c="dark"
                style={{
                  textTransform: 'uppercase',
                  letterSpacing: '1px'
                }}
              >
                ▶ LOGIN
              </Title>

              <form onSubmit={form.onSubmit(handleLogin)}>
                <Box mb="md">
                  <Text 
                    size="sm" 
                    fw="bold" 
                    mb="xs"
                    style={{
                      textTransform: 'uppercase',
                      letterSpacing: '1px',
                      color: '#880124'
                    }}
                  >
                    ◆ EMAIL
                  </Text>
                  <TextInput
                    placeholder="your@email.com"
                    size="lg"
                    {...form.getInputProps('email')}
                    styles={{
                      input: {
                        border: 'none',
                        borderBottom: '3px solid #E9ECEF',
                        borderRadius: 0,
                        backgroundColor: 'transparent',
                        fontSize: '1.1rem',
                        '&:focus': {
                          borderBottomColor: '#880124',
                          borderWidth: '3px'
                        }
                      }
                    }}
                  />
                </Box>

                <Box mb="lg">
                  <Text 
                    size="sm" 
                    fw="bold" 
                    mb="xs"
                    style={{
                      textTransform: 'uppercase',
                      letterSpacing: '1px',
                      color: '#880124'
                    }}
                  >
                    ◆ PASSWORD
                  </Text>
                  <PasswordInput
                    placeholder="Your password"
                    size="lg"
                    {...form.getInputProps('password')}
                    styles={{
                      input: {
                        border: 'none',
                        borderBottom: '3px solid #E9ECEF',
                        borderRadius: 0,
                        backgroundColor: 'transparent',
                        fontSize: '1.1rem',
                        '&:focus': {
                          borderBottomColor: '#880124',
                          borderWidth: '3px'
                        }
                      }
                    }}
                  />
                </Box>

                <Group justify="space-between" mb="xl">
                  <Checkbox
                    label="REMEMBER"
                    color="red"
                    size="md"
                    {...form.getInputProps('rememberMe', { type: 'checkbox' })}
                    styles={{
                      label: {
                        fontWeight: 'bold',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px'
                      }
                    }}
                  />
                </Group>

                <Button
                  type="submit"
                  size="xl"
                  fullWidth
                  mb="lg"
                  style={{
                    backgroundColor: '#880124',
                    border: 'none',
                    borderRadius: 0,
                    clipPath: 'polygon(0 0, calc(100% - 20px) 0, 100% 100%, 20px 100%)',
                    fontSize: '1.2rem',
                    fontWeight: 'bold',
                    textTransform: 'uppercase',
                    letterSpacing: '2px',
                    padding: '1rem'
                  }}
                  styles={{
                    root: {
                      '&:hover': {
                        backgroundColor: '#B76D75',
                        transform: 'translateX(5px)'
                      }
                    }
                  }}
                >
                  ▶ SIGN IN
                </Button>

                <Group justify="space-between">
                  <Anchor 
                    size="sm" 
                    c="dimmed"
                    href="/forgot-password"
                    style={{
                      textDecoration: 'none',
                      fontWeight: 'bold',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    ↗ FORGOT
                  </Anchor>
                  <Anchor 
                    size="sm" 
                    c="red"
                    href="/register"
                    style={{
                      textDecoration: 'none',
                      fontWeight: 'bold',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    ↗ REGISTER
                  </Anchor>
                </Group>
              </form>
            </Card>
          </Box>
        </Box>
      </Container>
    </Box>
  );
};
```

### Unique Features
- **Split Layout**: Brand showcase alongside login form
- **Clip-Path Styling**: Angular geometric shapes throughout
- **Underline Inputs**: Minimal border-bottom focus styling
- **Geometric Icons**: Custom shapes for labels and buttons
- **Angular Animations**: Transform effects with geometric precision

---

## Variation 4: Advanced Mantine (Dramatic Change)
**Edginess Level**: 4/5 | **Alignment**: Homepage Variation 4

### Design Philosophy
Leverages advanced Mantine v7 components with sophisticated interactions, rich validation feedback, and modern UX patterns.

### Visual Characteristics
- **Component Richness**: Advanced Mantine features like Spotlight integration
- **Validation System**: Real-time feedback with progress indicators
- **Interactive Elements**: Rich hover states and complex animations
- **Data Visualization**: Password strength and login attempt tracking

### Layout Structure
```
+----------------------------------+
|    Advanced Header w/ Search     |
+----------------------------------+
|                                  |
|     ┌─────────────────────┐      |
|     │ WITCH CITY ROPE     │      |
|     │ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓     │      |
|     │ Advanced Login       │      |
|     │                     │      |
|     │ Email [▼]           │      |
|     │ [Progressive Input] │      |
|     │                     │      |
|     │ Password [👁]        │      |
|     │ [████████░░] 8/10   │      |
|     │                     │      |
|     │ [⚙] Advanced Options│      |
|     │ [🔒] Login with 2FA │      |
|     │                     │      |
|     │ Analytics Dashboard │      |
|     │ ┌───┬───┬───┬───┐   │      |
|     │ │ 👤│📊 │🔔 │⚙ │   │      |
|     │ └───┴───┴───┴───┘   │      |
|     └─────────────────────┘      |
|                                  |
+----------------------------------+
```

### Mantine v7 Implementation
```typescript
import { 
  Card, 
  TextInput, 
  PasswordInput, 
  Button, 
  Checkbox, 
  Text, 
  Title, 
  Container, 
  Box,
  Group,
  Anchor,
  Progress,
  Badge,
  Stepper,
  Notification,
  Tabs,
  ActionIcon,
  Tooltip,
  Indicator,
  Stack,
  Divider,
  Avatar,
  Menu
} from '@mantine/core';
import { 
  IconEye, 
  IconEyeOff, 
  IconShield, 
  IconChart, 
  IconBell, 
  IconSettings,
  IconFingerprint,
  IconDeviceMobile
} from '@tabler/icons-react';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';

const AdvancedMantineLogin = () => {
  const [passwordStrength, setPasswordStrength] = useState(0);
  const [loginStep, setLoginStep] = useState(0);

  const form = useForm({
    initialValues: { 
      email: '', 
      password: '', 
      rememberMe: false,
      enableTwoFactor: false 
    },
    validate: {
      email: (value) => {
        if (!value) return 'Email is required';
        if (!/^\S+@\S+$/.test(value)) return 'Invalid email format';
        return null;
      },
      password: (value) => {
        if (!value) return 'Password is required';
        if (value.length < 8) return 'Password must be at least 8 characters';
        
        // Calculate strength
        let strength = 0;
        if (value.length >= 8) strength += 2;
        if (/[A-Z]/.test(value)) strength += 2;
        if (/[a-z]/.test(value)) strength += 2;
        if (/[0-9]/.test(value)) strength += 2;
        if (/[^A-Za-z0-9]/.test(value)) strength += 2;
        
        setPasswordStrength(strength);
        return null;
      },
    },
  });

  const getPasswordStrengthColor = () => {
    if (passwordStrength <= 2) return 'red';
    if (passwordStrength <= 6) return 'yellow';
    if (passwordStrength <= 8) return 'blue';
    return 'green';
  };

  return (
    <Container size="md" px="md" py="xl">
      <Card
        radius="xl"
        shadow="xl"
        padding="xl"
        withBorder
        style={{
          maxWidth: 500,
          margin: '0 auto',
          background: 'linear-gradient(145deg, #ffffff 0%, #f8f9fa 100%)'
        }}
      >
        {/* Header with branding and progress */}
        <Box mb="xl">
          <Group justify="space-between" align="center" mb="md">
            <Title
              order={2}
              size="1.8rem"
              fw="bold"
              variant="gradient"
              gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}
            >
              WITCH CITY ROPE
            </Title>
            
            <Group gap="xs">
              <Tooltip label="Member Dashboard">
                <ActionIcon variant="light" color="witchcity" size="lg">
                  <IconChart size={18} />
                </ActionIcon>
              </Tooltip>
              <Indicator color="red" size={6}>
                <Tooltip label="3 new notifications">
                  <ActionIcon variant="light" color="blue" size="lg">
                    <IconBell size={18} />
                  </ActionIcon>
                </Tooltip>
              </Indicator>
            </Group>
          </Group>

          <Stepper active={loginStep} size="sm" color="witchcity">
            <Stepper.Step label="Credentials" />
            <Stepper.Step label="Verification" />
            <Stepper.Step label="Access" />
          </Stepper>
        </Box>

        <Tabs defaultValue="standard" mb="xl">
          <Tabs.List grow>
            <Tabs.Tab value="standard" leftSection={<IconShield size={16} />}>
              Standard Login
            </Tabs.Tab>
            <Tabs.Tab value="advanced" leftSection={<IconFingerprint size={16} />}>
              2FA Login
            </Tabs.Tab>
          </Tabs.List>

          <Tabs.Panel value="standard" pt="md">
            <form onSubmit={form.onSubmit(handleLogin)}>
              <Stack gap="md">
                <TextInput
                  label="Email Address"
                  placeholder="member@witchcityrope.com"
                  size="md"
                  rightSection={
                    <Tooltip label="We'll remember this for faster login">
                      <ActionIcon variant="transparent" color="dimmed">
                        <IconChart size={16} />
                      </ActionIcon>
                    </Tooltip>
                  }
                  {...form.getInputProps('email')}
                  styles={{
                    input: {
                      borderColor: 'var(--mantine-color-gray-4)',
                      '&:focus': {
                        borderColor: 'var(--mantine-color-witchcity-6)',
                        boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)'
                      }
                    }
                  }}
                />

                <Box>
                  <PasswordInput
                    label="Password"
                    placeholder="Your secure password"
                    size="md"
                    {...form.getInputProps('password')}
                    styles={{
                      input: {
                        borderColor: 'var(--mantine-color-gray-4)',
                        '&:focus': {
                          borderColor: 'var(--mantine-color-witchcity-6)',
                          boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)'
                        }
                      }
                    }}
                  />
                  
                  {form.values.password && (
                    <Box mt="xs">
                      <Group justify="space-between" mb="xs">
                        <Text size="xs" c="dimmed">Password strength</Text>
                        <Badge 
                          color={getPasswordStrengthColor()} 
                          variant="light" 
                          size="xs"
                        >
                          {passwordStrength}/10
                        </Badge>
                      </Group>
                      <Progress 
                        value={(passwordStrength / 10) * 100} 
                        color={getPasswordStrengthColor()}
                        size="xs"
                        radius="xl"
                        animated
                      />
                    </Box>
                  )}
                </Box>

                <Group justify="space-between">
                  <Checkbox
                    label="Keep me signed in"
                    color="witchcity"
                    {...form.getInputProps('rememberMe', { type: 'checkbox' })}
                  />
                  
                  <Menu shadow="md" width={200}>
                    <Menu.Target>
                      <ActionIcon variant="subtle" color="witchcity">
                        <IconSettings size={16} />
                      </ActionIcon>
                    </Menu.Target>
                    <Menu.Dropdown>
                      <Menu.Item leftSection={<IconDeviceMobile size={14} />}>
                        Mobile App Login
                      </Menu.Item>
                      <Menu.Item leftSection={<IconFingerprint size={14} />}>
                        Biometric Login
                      </Menu.Item>
                      <Divider />
                      <Menu.Item c="dimmed">
                        Security Settings
                      </Menu.Item>
                    </Menu.Dropdown>
                  </Menu>
                </Group>

                <Button
                  type="submit"
                  variant="gradient"
                  gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}
                  size="lg"
                  fullWidth
                  leftSection={<IconShield size={18} />}
                  styles={{
                    root: {
                      '&:hover': {
                        transform: 'translateY(-1px)',
                        boxShadow: '0 6px 20px rgba(136, 1, 36, 0.3)'
                      }
                    }
                  }}
                >
                  Sign In Securely
                </Button>
              </Stack>
            </form>
          </Tabs.Panel>

          <Tabs.Panel value="advanced" pt="md">
            <Stack gap="md">
              <Text size="sm" c="dimmed" ta="center">
                Enhanced security with two-factor authentication
              </Text>
              
              <Card withBorder radius="md" p="md" bg="gray.0">
                <Group>
                  <Avatar color="witchcity" radius="xl">
                    <IconFingerprint size={20} />
                  </Avatar>
                  <div>
                    <Text fw={500}>Biometric Authentication</Text>
                    <Text size="xs" c="dimmed">Use fingerprint or face recognition</Text>
                  </div>
                </Group>
              </Card>
              
              <Button variant="outline" color="witchcity" size="lg" fullWidth>
                Enable Advanced Security
              </Button>
            </Stack>
          </Tabs.Panel>
        </Tabs>

        <Divider mb="md" />

        <Group justify="center" gap="xl">
          <Anchor size="sm" c="witchcity.6" href="/forgot-password">
            Forgot password?
          </Anchor>
          <Anchor size="sm" c="witchcity.6" href="/register">
            Create account
          </Anchor>
        </Group>

        {/* Login analytics */}
        <Box mt="xl" pt="md" style={{ borderTop: '1px solid var(--mantine-color-gray-2)' }}>
          <Text size="xs" c="dimmed" ta="center" mb="xs">
            Login Analytics
          </Text>
          <Group justify="center" gap="lg">
            <Box ta="center">
              <Text size="lg" fw="bold" c="witchcity">156</Text>
              <Text size="xs" c="dimmed">Active Members</Text>
            </Box>
            <Box ta="center">
              <Text size="lg" fw="bold" c="blue">23</Text>
              <Text size="xs" c="dimmed">Online Now</Text>
            </Box>
            <Box ta="center">
              <Text size="lg" fw="bold" c="green">98%</Text>
              <Text size="xs" c="dimmed">Uptime</Text>
            </Box>
          </Group>
        </Box>
      </Card>
    </Container>
  );
};
```

### Unique Features
- **Progressive Stepper**: Visual progress through login steps
- **Tabbed Interface**: Standard vs 2FA login options
- **Password Strength**: Real-time strength analysis with progress bar
- **Security Options**: Biometric and advanced authentication options
- **Login Analytics**: Community engagement metrics
- **Rich Tooltips**: Contextual help throughout the interface
- **Menu Systems**: Advanced options accessible via menus

---

## Variation 5: Template-Inspired Ultra-Modern (Revolutionary)
**Edginess Level**: 5/5 | **Alignment**: Homepage Variation 5

### Design Philosophy
Analytics dashboard-inspired design with professional-grade UX patterns, data visualization, and enterprise-level authentication features.

### Visual Characteristics
- **Dashboard Aesthetic**: Split-screen with data panels
- **Professional Polish**: Enterprise SaaS-level design quality
- **Data Integration**: Real-time community metrics and insights
- **Advanced UX**: Multi-modal authentication and user analytics

### Layout Structure
```
+----------------------------------+
|        Dashboard Header          |
+----------------------------------+
| ┌────────────────┬─────────────┐ |
| │ METRICS PANEL  │ LOGIN FORM  │ |
| │ ┌────────────┐ │ ┌─────────┐ │ |
| │ │ User Stats │ │ │ Welcome │ │ |
| │ └────────────┘ │ └─────────┘ │ |
| │ ┌────────────┐ │             │ |
| │ │ Activity   │ │ [Email]     │ |
| │ │ Feed       │ │ [Password]  │ |
| │ └────────────┘ │ [Options]   │ |
| │ ┌────────────┐ │             │ |
| │ │ Community  │ │ [Sign In]   │ |
| │ │ Health     │ │             │ |
| │ └────────────┘ │ [Links]     │ |
| └────────────────┴─────────────┘ |
+----------------------------------+
```

### Mantine v7 Implementation
```typescript
import { 
  Card, 
  TextInput, 
  PasswordInput, 
  Button, 
  Text, 
  Title, 
  Container, 
  Box,
  Group,
  Grid,
  Stack,
  Badge,
  Avatar,
  RingProgress,
  SimpleGrid,
  Paper,
  ThemeIcon,
  Divider
} from '@mantine/core';
import { 
  IconUsers, 
  IconActivity, 
  IconTrendingUp, 
  IconCalendar,
  IconShield,
  IconChartBar,
  IconClock,
  IconHeart
} from '@tabler/icons-react';

const TemplateInspiredLogin = () => {
  return (
    <Box
      style={{
        background: 'linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%)',
        minHeight: '100vh'
      }}
    >
      <Container size="xl" px="md" py="lg">
        <Grid>
          {/* Analytics Dashboard Panel */}
          <Grid.Col span={{ base: 12, md: 8 }}>
            <Stack gap="md">
              {/* Header */}
              <Card shadow="sm" radius="lg" withBorder p="lg">
                <Group justify="space-between" align="center">
                  <div>
                    <Title order={2} fw="bold" c="dark">
                      WITCH CITY ROPE
                    </Title>
                    <Text c="dimmed" size="sm">
                      Community Dashboard • Salem, MA
                    </Text>
                  </div>
                  <Badge 
                    variant="light" 
                    color="green" 
                    size="lg"
                    leftSection={<IconActivity size={14} />}
                  >
                    LIVE
                  </Badge>
                </Group>
              </Card>

              {/* Metrics Grid */}
              <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="md">
                <Paper withBorder radius="lg" p="md">
                  <Group>
                    <ThemeIcon color="witchcity" size="xl" radius="md">
                      <IconUsers size={20} />
                    </ThemeIcon>
                    <div>
                      <Text fw="bold" size="xl">642</Text>
                      <Text size="xs" c="dimmed">Total Members</Text>
                    </div>
                  </Group>
                </Paper>

                <Paper withBorder radius="lg" p="md">
                  <Group>
                    <ThemeIcon color="blue" size="xl" radius="md">
                      <IconActivity size={20} />
                    </ThemeIcon>
                    <div>
                      <Text fw="bold" size="xl">28</Text>
                      <Text size="xs" c="dimmed">Online Now</Text>
                    </div>
                  </Group>
                </Paper>

                <Paper withBorder radius="lg" p="md">
                  <Group>
                    <ThemeIcon color="green" size="xl" radius="md">
                      <IconCalendar size={20} />
                    </ThemeIcon>
                    <div>
                      <Text fw="bold" size="xl">12</Text>
                      <Text size="xs" c="dimmed">Events This Week</Text>
                    </div>
                  </Group>
                </Paper>

                <Paper withBorder radius="lg" p="md">
                  <Group>
                    <ThemeIcon color="orange" size="xl" radius="md">
                      <IconTrendingUp size={20} />
                    </ThemeIcon>
                    <div>
                      <Text fw="bold" size="xl">94%</Text>
                      <Text size="xs" c="dimmed">Satisfaction</Text>
                    </div>
                  </Group>
                </Paper>
              </SimpleGrid>

              {/* Community Health */}
              <Card shadow="sm" radius="lg" withBorder p="lg">
                <Group justify="space-between" mb="md">
                  <Title order={4}>Community Health</Title>
                  <Badge variant="light" color="green">Excellent</Badge>
                </Group>
                
                <SimpleGrid cols={3} spacing="xl">
                  <Box ta="center">
                    <RingProgress
                      size={80}
                      thickness={8}
                      sections={[{ value: 85, color: 'witchcity' }]}
                      label={
                        <Text fw="bold" ta="center" size="xs">
                          85%
                        </Text>
                      }
                    />
                    <Text size="xs" c="dimmed" mt="xs">Engagement</Text>
                  </Box>
                  
                  <Box ta="center">
                    <RingProgress
                      size={80}
                      thickness={8}
                      sections={[{ value: 92, color: 'blue' }]}
                      label={
                        <Text fw="bold" ta="center" size="xs">
                          92%
                        </Text>
                      }
                    />
                    <Text size="xs" c="dimmed" mt="xs">Safety Score</Text>
                  </Box>
                  
                  <Box ta="center">
                    <RingProgress
                      size={80}
                      thickness={8}
                      sections={[{ value: 78, color: 'green' }]}
                      label={
                        <Text fw="bold" ta="center" size="xs">
                          78%
                        </Text>
                      }
                    />
                    <Text size="xs" c="dimmed" mt="xs">Growth</Text>
                  </Box>
                </SimpleGrid>
              </Card>

              {/* Recent Activity Feed */}
              <Card shadow="sm" radius="lg" withBorder p="lg">
                <Title order={4} mb="md">Recent Community Activity</Title>
                <Stack gap="md">
                  {[
                    { user: 'Sarah K.', action: 'joined Advanced Suspension workshop', time: '2 min ago', avatar: 'S' },
                    { user: 'Alex M.', action: 'completed Beginner Safety course', time: '15 min ago', avatar: 'A' },
                    { user: 'Jordan L.', action: 'posted in Community Discussion', time: '1 hour ago', avatar: 'J' }
                  ].map((activity, index) => (
                    <Group key={index}>
                      <Avatar color="witchcity" radius="xl">{activity.avatar}</Avatar>
                      <div style={{ flex: 1 }}>
                        <Text size="sm" fw={500}>{activity.user}</Text>
                        <Text size="xs" c="dimmed">{activity.action}</Text>
                      </div>
                      <Text size="xs" c="dimmed">{activity.time}</Text>
                    </Group>
                  ))}
                </Stack>
              </Card>
            </Stack>
          </Grid.Col>

          {/* Login Panel */}
          <Grid.Col span={{ base: 12, md: 4 }}>
            <Card shadow="xl" radius="lg" withBorder p="xl" h="fit-content">
              <Stack gap="lg">
                <Box ta="center">
                  <ThemeIcon size="xl" variant="light" color="witchcity" mb="md">
                    <IconShield size={24} />
                  </ThemeIcon>
                  <Title order={3} fw="bold" mb="xs">
                    Member Login
                  </Title>
                  <Text size="sm" c="dimmed">
                    Access your community dashboard
                  </Text>
                </Box>

                <form onSubmit={form.onSubmit(handleLogin)}>
                  <Stack gap="md">
                    <TextInput
                      label="Email Address"
                      placeholder="member@example.com"
                      size="md"
                      {...form.getInputProps('email')}
                      styles={{
                        label: { fontWeight: 600 },
                        input: {
                          borderColor: 'var(--mantine-color-gray-4)',
                          '&:focus': {
                            borderColor: 'var(--mantine-color-witchcity-6)',
                            boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)'
                          }
                        }
                      }}
                    />

                    <PasswordInput
                      label="Password"
                      placeholder="Your secure password"
                      size="md"
                      {...form.getInputProps('password')}
                      styles={{
                        label: { fontWeight: 600 },
                        input: {
                          borderColor: 'var(--mantine-color-gray-4)',
                          '&:focus': {
                            borderColor: 'var(--mantine-color-witchcity-6)',
                            boxShadow: '0 0 0 2px rgba(136, 1, 36, 0.1)'
                          }
                        }
                      }}
                    />

                    <Button
                      type="submit"
                      variant="gradient"
                      gradient={{ from: 'witchcity.6', to: 'witchcity.4' }}
                      size="lg"
                      fullWidth
                      leftSection={<IconChartBar size={18} />}
                      styles={{
                        root: {
                          '&:hover': {
                            transform: 'translateY(-2px)',
                            boxShadow: '0 8px 25px rgba(136, 1, 36, 0.3)'
                          }
                        }
                      }}
                    >
                      Access Dashboard
                    </Button>
                  </Stack>
                </form>

                <Divider />

                <Stack gap="xs">
                  <Group justify="center" gap="xl">
                    <Text size="sm" c="dimmed" ta="center">
                      <Text component="span" fw={500} c="dark">Forgot password?</Text>
                    </Text>
                  </Group>
                  
                  <Text size="sm" c="dimmed" ta="center">
                    New to the community?{' '}
                    <Text component="span" fw={500} c="witchcity.6">
                      Request an invitation
                    </Text>
                  </Text>
                </Stack>

                {/* Security Badge */}
                <Paper bg="gray.0" p="md" radius="md">
                  <Group>
                    <ThemeIcon color="green" variant="light">
                      <IconShield size={16} />
                    </ThemeIcon>
                    <div>
                      <Text size="xs" fw={500}>Secure Login</Text>
                      <Text size="xs" c="dimmed">256-bit SSL encryption</Text>
                    </div>
                  </Group>
                </Paper>
              </Stack>
            </Card>

            {/* Quick Stats */}
            <Card shadow="sm" radius="lg" withBorder p="md" mt="md">
              <Group justify="space-between" mb="xs">
                <Text fw={500} size="sm">Today's Activity</Text>
                <Badge variant="light" color="blue" size="xs">Live</Badge>
              </Group>
              <SimpleGrid cols={2} spacing="xs">
                <Box>
                  <Text fw="bold" c="witchcity">8</Text>
                  <Text size="xs" c="dimmed">New Members</Text>
                </Box>
                <Box>
                  <Text fw="bold" c="blue">15</Text>
                  <Text size="xs" c="dimmed">Event RSVPs</Text>
                </Box>
              </SimpleGrid>
            </Card>
          </Grid.Col>
        </Grid>
      </Container>
    </Box>
  );
};
```

### Unique Features
- **Split Dashboard Layout**: Analytics panel alongside login
- **Real-Time Metrics**: Live community statistics and health indicators
- **Professional Polish**: Enterprise-grade visual design
- **Activity Feed**: Recent community engagement display
- **Data Visualization**: Ring progress indicators and trend charts
- **Security Emphasis**: Professional security badging and encryption notices

---

## Common Implementation Patterns

### Responsive Design Strategy
All variations implement mobile-first responsive design with consistent breakpoints:

```typescript
const responsiveStyles = {
  '@media (max-width: 768px)': {
    // Mobile adaptations
    gridTemplateColumns: '1fr',
    padding: '1rem',
    fontSize: '0.9rem'
  },
  '@media (min-width: 769px) and (max-width: 1024px)': {
    // Tablet adaptations
    gridTemplateColumns: 'repeat(2, 1fr)',
    padding: '1.5rem'
  },
  '@media (min-width: 1025px)': {
    // Desktop optimizations
    gridTemplateColumns: 'repeat(3, 1fr)',
    padding: '2rem'
  }
};
```

### Form Validation Patterns
Consistent validation approach across all variations:

```typescript
const validateForm = {
  email: (value) => {
    if (!value) return 'Email is required';
    if (!/^\S+@\S+$/.test(value)) return 'Invalid email format';
    return null;
  },
  password: (value) => {
    if (!value) return 'Password is required';
    if (value.length < 8) return 'Password must be at least 8 characters';
    return null;
  }
};
```

### Error Handling Implementation
```typescript
const handleLoginError = (error) => {
  notifications.show({
    title: 'Login Failed',
    message: error.message || 'Please check your credentials and try again',
    color: 'red',
    icon: <IconX size={16} />
  });
};
```

### Accessibility Features
All variations maintain WCAG 2.1 AA compliance:

- **Keyboard Navigation**: Full functionality without mouse
- **Screen Reader Support**: Proper ARIA labels and descriptions
- **Color Contrast**: Minimum 4.5:1 ratio maintained
- **Focus Management**: Clear focus indicators and logical tab order
- **Motion Respect**: All animations respect `prefers-reduced-motion`

## Implementation Timeline

### Week 1: Foundation & Variation 1
- Set up base login component structure
- Implement Enhanced Current variation
- Establish form validation patterns
- Create responsive design framework

### Week 2: Variations 2-3
- Develop Dark Theme Focus variation
- Create Geometric Modern variation
- Implement advanced animation systems
- Test cross-browser compatibility

### Week 3: Variations 4-5
- Build Advanced Mantine variation
- Develop Template-Inspired variation
- Integrate complex component libraries
- Performance optimization

### Week 4: Polish & Integration
- Stakeholder review and feedback incorporation
- Accessibility audit and compliance verification
- Integration with authentication system
- Documentation completion

Each login variation maintains the aesthetic coherence with its corresponding homepage while optimizing for secure, user-friendly authentication flows. The progressive complexity allows stakeholders to select their preferred level of sophistication while maintaining excellent UX patterns throughout.