# Component Library - Design System v7

**Authority**: Based on approved Final Design v7 template
**Target**: React + Mantine v7 development
**Last Updated**: 2025-08-20

## Overview

This component library provides React implementations of all v7 design system components with Mantine v7 integration. Each component is production-ready and includes TypeScript definitions, accessibility features, and responsive design.

## Component Architecture

### Design Principles
- **Mantine v7 First**: Built on Mantine components as foundation
- **Design System Authority**: v7 styles override Mantine defaults
- **TypeScript Native**: Full type safety and IntelliSense
- **Accessibility Required**: WCAG 2.1 AA compliance
- **Mobile Optimized**: Responsive and touch-friendly

### Folder Structure
```
src/components/
├── ui/                 # Core design system components
│   ├── Button/
│   ├── NavLink/
│   ├── FeatureIcon/
│   ├── EventCard/
│   └── SectionTitle/
├── layout/             # Layout and structural components
│   ├── Header/
│   ├── Footer/
│   ├── PageLayout/
│   └── Section/
├── forms/              # Form-specific components
│   ├── FormField/
│   ├── LoginForm/
│   └── SearchInput/
└── content/            # Content-specific components
    ├── EventGrid/
    ├── FeatureGrid/
    └── HeroSection/
```

## Core UI Components

### Button Component

**File**: `src/components/ui/Button/Button.tsx`

```typescript
import React from 'react';
import { Button as MantineButton, ButtonProps as MantineButtonProps } from '@mantine/core';
import { styled } from '@emotion/styled';

// Styled button with v7 design system
const StyledButton = styled(MantineButton)<{ variant?: 'primary' | 'electric' | 'secondary' }>`
  /* Base button styles */
  border-radius: 12px 6px 12px 6px;
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 14px;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  transition: all 0.3s ease;
  border: none;
  cursor: pointer;
  position: relative;
  overflow: hidden;
  
  /* Signature corner animation */
  &:hover {
    border-radius: 6px 12px 6px 12px;
    transform: none; /* CRITICAL: No vertical movement */
  }
  
  /* Primary amber variant */
  &.btn-primary {
    background: linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%);
    color: var(--color-midnight);
    box-shadow: 0 4px 15px rgba(255, 191, 0, 0.4);
    
    &:hover {
      background: linear-gradient(135deg, var(--color-amber-dark) 0%, var(--color-amber) 100%);
      box-shadow: 0 6px 25px rgba(255, 191, 0, 0.5);
    }
    
    /* Shimmer effect */
    &::before {
      content: '';
      position: absolute;
      top: 0;
      left: -100%;
      width: 100%;
      height: 100%;
      background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
      transition: left 0.5s ease;
    }
    
    &:hover::before {
      left: 100%;
    }
  }
  
  /* Electric purple variant */
  &.btn-electric {
    background: linear-gradient(135deg, var(--color-electric) 0%, var(--color-electric-dark) 100%);
    color: var(--color-ivory);
    box-shadow: 0 4px 15px rgba(157, 78, 221, 0.4);
    font-weight: 700;
    
    &:hover {
      background: linear-gradient(135deg, var(--color-electric-dark) 0%, var(--color-electric) 100%);
      box-shadow: 0 6px 25px rgba(157, 78, 221, 0.5);
    }
  }
  
  /* Secondary outlined variant */
  &.btn-secondary {
    background: transparent;
    color: var(--color-burgundy);
    border: 2px solid var(--color-burgundy);
    position: relative;
    z-index: 1;
    
    &::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      width: 0;
      height: 100%;
      background: var(--color-burgundy);
      transition: width 0.4s ease;
      z-index: -1;
    }
    
    &:hover {
      color: var(--color-ivory);
      
      &::before {
        width: 100%;
      }
    }
  }
  
  /* Large variant */
  &.btn-large {
    padding: 18px 40px;
    font-size: 16px;
  }
  
  /* Mobile optimizations */
  @media (max-width: 768px) {
    padding: 12px 24px;
    font-size: 13px;
    
    &.btn-large {
      padding: 16px 32px;
      font-size: 15px;
    }
  }
`;

interface ButtonProps extends Omit<MantineButtonProps, 'variant'> {
  variant?: 'primary' | 'electric' | 'secondary';
  large?: boolean;
  children: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({ 
  variant = 'primary', 
  large = false, 
  children, 
  className = '',
  ...props 
}) => {
  const buttonClass = `btn-${variant} ${large ? 'btn-large' : ''} ${className}`.trim();
  
  return (
    <StyledButton 
      className={buttonClass}
      variant={variant}
      {...props}
    >
      {children}
    </StyledButton>
  );
};

// Usage examples
export const ButtonExamples = () => (
  <>
    <Button variant="primary">Join Community</Button>
    <Button variant="electric">Browse Classes</Button>
    <Button variant="secondary">Learn More</Button>
    <Button variant="primary" large>Hero CTA</Button>
  </>
);
```

**Props Interface**:
```typescript
interface ButtonProps {
  variant?: 'primary' | 'electric' | 'secondary';
  large?: boolean;
  disabled?: boolean;
  loading?: boolean;
  onClick?: (event: React.MouseEvent) => void;
  children: React.ReactNode;
  className?: string;
  type?: 'button' | 'submit' | 'reset';
  'aria-label'?: string;
}
```

**Usage Guidelines**:
- Use `primary` for main actions (join, register, login)
- Use `electric` for secondary important actions (browse, explore)
- Use `secondary` for less prominent actions (learn more, cancel)
- Add `large` prop for hero sections and major CTAs
- Always include `aria-label` for accessibility

### NavLink Component

**File**: `src/components/ui/NavLink/NavLink.tsx`

```typescript
import React from 'react';
import { Anchor, AnchorProps } from '@mantine/core';
import { styled } from '@emotion/styled';

const StyledNavLink = styled(Anchor)`
  color: var(--color-charcoal);
  text-decoration: none;
  font-family: var(--font-heading);
  font-weight: 500;
  font-size: 15px;
  text-transform: uppercase;
  letter-spacing: 1px;
  transition: all 0.3s ease;
  position: relative;
  display: inline-block;
  
  /* Center-outward underline animation */
  &::after {
    content: '';
    position: absolute;
    bottom: -4px;
    left: 50%;
    transform: translateX(-50%);
    width: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
    transition: width 0.3s ease;
  }
  
  &:hover {
    color: var(--color-burgundy);
    text-decoration: none;
    
    &::after {
      width: 100%;
    }
  }
  
  /* Active state */
  &.active {
    color: var(--color-burgundy);
    
    &::after {
      width: 100%;
      background: var(--color-burgundy);
    }
  }
  
  /* Mobile styles */
  @media (max-width: 768px) {
    font-size: 14px;
    padding: 8px 0;
  }
`;

interface NavLinkProps extends AnchorProps {
  active?: boolean;
  children: React.ReactNode;
}

export const NavLink: React.FC<NavLinkProps> = ({ 
  active = false, 
  children, 
  className = '',
  ...props 
}) => {
  const linkClass = `${active ? 'active' : ''} ${className}`.trim();
  
  return (
    <StyledNavLink className={linkClass} {...props}>
      {children}
    </StyledNavLink>
  );
};

// Usage examples
export const NavLinkExamples = () => (
  <nav style={{ display: 'flex', gap: 'var(--space-lg)' }}>
    <NavLink href="#events">Events & Classes</NavLink>
    <NavLink href="#join" active>How to Join</NavLink>
    <NavLink href="#resources">Resources</NavLink>
  </nav>
);
```

### FeatureIcon Component

**File**: `src/components/ui/FeatureIcon/FeatureIcon.tsx`

```typescript
import React from 'react';
import { Box, Text } from '@mantine/core';
import { styled } from '@emotion/styled';

const FeatureContainer = styled(Box)`
  text-align: center;
  position: relative;
`;

const IconContainer = styled(Box)`
  width: 100px;
  height: 100px;
  background: linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-rose-gold) 100%);
  border-radius: 50% 20% 50% 20%; /* Organic asymmetric shape */
  margin: 0 auto var(--space-md);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-ivory);
  font-size: 48px;
  box-shadow: 0 10px 30px rgba(136, 1, 36, 0.2);
  position: relative;
  overflow: hidden;
  transition: all 0.5s ease; /* Slower for dramatic effect */
  cursor: pointer;
  
  /* Radial highlight overlay */
  &::before {
    content: '';
    position: absolute;
    top: -50%;
    left: -50%;
    width: 200%;
    height: 200%;
    background: radial-gradient(circle, rgba(255,255,255,0.3) 0%, transparent 70%);
    opacity: 0;
    transition: opacity 0.3s ease;
  }
  
  ${FeatureContainer}:hover & {
    border-radius: 20% 50% 20% 50%; /* Shape morphs */
    transform: rotate(5deg) scale(1.1); /* Rotation + scale */
    
    &::before {
      opacity: 1; /* Highlight appears */
    }
  }
  
  /* Mobile optimization */
  @media (max-width: 768px) {
    width: 80px;
    height: 80px;
    font-size: 36px;
    
    ${FeatureContainer}:hover & {
      transform: scale(1.05); /* Less dramatic on mobile */
    }
  }
`;

const FeatureTitle = styled(Text)`
  font-family: var(--font-heading);
  font-size: 24px;
  margin-bottom: var(--space-sm);
  color: var(--color-burgundy);
  text-transform: uppercase;
  letter-spacing: 1px;
  font-weight: 700;
  
  @media (max-width: 768px) {
    font-size: 20px;
  }
`;

const FeatureDescription = styled(Text)`
  color: var(--color-charcoal);
  line-height: 1.7;
  font-size: 16px;
  font-family: var(--font-body);
  
  @media (max-width: 768px) {
    font-size: 14px;
    line-height: 1.6;
  }
`;

interface FeatureIconProps {
  icon: React.ReactNode | string;
  title: string;
  description: string;
  className?: string;
}

export const FeatureIcon: React.FC<FeatureIconProps> = ({ 
  icon, 
  title, 
  description, 
  className = '' 
}) => {
  return (
    <FeatureContainer className={className}>
      <IconContainer>
        {typeof icon === 'string' ? (
          <span role="img" aria-label={title}>{icon}</span>
        ) : (
          icon
        )}
      </IconContainer>
      <FeatureTitle as="h3">{title}</FeatureTitle>
      <FeatureDescription>{description}</FeatureDescription>
    </FeatureContainer>
  );
};

// Usage examples
export const FeatureIconExamples = () => (
  <div style={{ 
    display: 'grid', 
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', 
    gap: 'var(--space-xl)' 
  }}>
    <FeatureIcon
      icon="🎭"
      title="Expert Teaching"
      description="Learn from experienced instructors who make rope accessible to everyone. We blend technical skills with creativity and always prioritize safety."
    />
    <FeatureIcon
      icon="🌹"
      title="Welcoming Community"
      description="Find your people in our warm, respectful space. We celebrate diversity, honor boundaries, and support each other's growth."
    />
    <FeatureIcon
      icon="🔮"
      title="Safety First, Always"
      description="We're committed to risk-aware practice, ongoing consent, and creating an environment where everyone feels secure to learn and explore."
    />
    <FeatureIcon
      icon="✨"
      title="Everyone Belongs"
      description="All bodies, orientations, and experience levels are welcome here. Come as you are—we're excited to support your unique journey."
    />
  </div>
);
```

### EventCard Component

**File**: `src/components/ui/EventCard/EventCard.tsx`

```typescript
import React from 'react';
import { Card, Text, Group, Badge } from '@mantine/core';
import { styled } from '@emotion/styled';

const StyledCard = styled(Card)`
  background: white;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0,0,0,0.05);
  transition: all 0.3s ease;
  text-decoration: none;
  display: block;
  cursor: pointer;
  
  &:hover {
    transform: translateY(-4px); /* Card lift effect */
    box-shadow: 0 8px 24px rgba(0,0,0,0.1);
    text-decoration: none;
  }
  
  @media (max-width: 768px) {
    margin-bottom: var(--space-md);
  }
`;

const EventImage = styled.div`
  height: 100px;
  background: linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%);
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-md);
  
  &::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: url("data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M10,50 Q30,20 50,50 T90,50' stroke='%23FFFFFF' stroke-width='0.5' fill='none' opacity='0.2'/%3E%3C/svg%3E");
    background-size: 200px 200px;
  }
`;

const EventImageTitle = styled(Text)`
  font-family: var(--font-heading);
  font-size: 22px;
  font-weight: 700;
  color: var(--color-ivory);
  line-height: 1.3;
  text-align: center;
  position: relative;
  z-index: 1;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
  
  @media (max-width: 768px) {
    font-size: 18px;
  }
`;

const EventContent = styled.div`
  padding: var(--space-lg);
  
  @media (max-width: 768px) {
    padding: var(--space-md);
  }
`;

const EventDate = styled(Text)`
  font-family: var(--font-heading);
  font-size: 14px;
  font-weight: 600;
  color: var(--color-burgundy);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: var(--space-xs);
`;

const EventDescription = styled(Text)`
  color: var(--color-stone);
  font-size: 15px;
  line-height: 1.6;
  margin-bottom: var(--space-md);
  font-family: var(--font-body);
  
  @media (max-width: 768px) {
    font-size: 14px;
  }
`;

const EventDetails = styled(Group)`
  font-size: 14px;
  color: var(--color-smoke);
  margin-bottom: var(--space-md);
  flex-wrap: wrap;
  gap: var(--space-sm);
  
  @media (max-width: 768px) {
    font-size: 13px;
    gap: var(--space-xs);
  }
`;

const EventFooter = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: var(--space-md);
  border-top: 1px solid var(--color-taupe);
  
  @media (max-width: 768px) {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--space-xs);
  }
`;

const EventPrice = styled(Text)`
  font-family: var(--font-heading);
  font-size: 18px;
  font-weight: 700;
  color: var(--color-burgundy);
  
  @media (max-width: 768px) {
    font-size: 16px;
  }
`;

interface EventDetail {
  icon: string;
  text: string;
}

interface EventCardProps {
  title: string;
  date: string;
  description: string;
  price: string;
  status: 'available' | 'limited' | 'full';
  statusText: string;
  details: EventDetail[];
  href?: string;
  onClick?: () => void;
  className?: string;
}

const getStatusColor = (status: EventCardProps['status']) => {
  switch (status) {
    case 'available':
      return 'var(--color-success)';
    case 'limited':
      return 'var(--color-warning)';
    case 'full':
      return 'var(--color-error)';
    default:
      return 'var(--color-stone)';
  }
};

export const EventCard: React.FC<EventCardProps> = ({
  title,
  date,
  description,
  price,
  status,
  statusText,
  details,
  href,
  onClick,
  className = ''
}) => {
  const cardProps = {
    className,
    ...(href ? { component: 'a', href } : {}),
    ...(onClick ? { onClick } : {})
  };

  return (
    <StyledCard {...cardProps}>
      <EventImage>
        <EventImageTitle>{title}</EventImageTitle>
      </EventImage>
      
      <EventContent>
        <EventDate>{date}</EventDate>
        <EventDescription>{description}</EventDescription>
        
        <EventDetails>
          {details.map((detail, index) => (
            <Text key={index} size="sm">
              {detail.icon} {detail.text}
            </Text>
          ))}
        </EventDetails>
        
        <EventFooter>
          <EventPrice>{price}</EventPrice>
          <Badge
            variant="light"
            style={{
              backgroundColor: 'transparent',
              color: getStatusColor(status),
              fontWeight: 600,
              fontSize: '14px'
            }}
          >
            {statusText}
          </Badge>
        </EventFooter>
      </EventContent>
    </StyledCard>
  );
};

// Usage examples
export const EventCardExamples = () => (
  <div style={{ 
    display: 'grid', 
    gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))', 
    gap: 'var(--space-lg)' 
  }}>
    <EventCard
      title="Rope Fundamentals"
      date="Saturday, March 15 • 2:00 PM"
      description="Learn fundamental ties and safety practices in a supportive environment. Perfect for your first class or building strong foundations."
      price="$35-55"
      status="available"
      statusText="10 spots left"
      details={[
        { icon: '📍', text: 'Salem Studio' },
        { icon: '⏱️', text: '2.5 hours' },
        { icon: '📚', text: 'Beginner' }
      ]}
      href="#event-1"
    />
    <EventCard
      title="Suspension Intensive"
      date="Sunday, March 23 • 1:00 PM"
      description="Take your skills to new heights! This workshop explores suspension basics with a strong focus on safety. Prerequisites apply."
      price="$95"
      status="limited"
      statusText="3 spots left"
      details={[
        { icon: '📍', text: 'Salem Studio' },
        { icon: '⏱️', text: '4 hours' },
        { icon: '📚', text: 'Advanced' }
      ]}
      href="#event-2"
    />
  </div>
);
```

### SectionTitle Component

**File**: `src/components/ui/SectionTitle/SectionTitle.tsx`

```typescript
import React from 'react';
import { Title, TitleProps } from '@mantine/core';
import { styled } from '@emotion/styled';

const StyledTitle = styled(Title)`
  font-family: var(--font-heading);
  font-size: 48px;
  font-weight: 800;
  margin-bottom: var(--space-xl);
  text-align: center;
  color: var(--color-burgundy);
  position: relative;
  text-transform: uppercase;
  letter-spacing: 3px;
  
  /* Gradient underline */
  &::after {
    content: '';
    display: block;
    width: 100px;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--color-rose-gold), transparent);
    margin: var(--space-sm) auto 0;
  }
  
  @media (max-width: 768px) {
    font-size: 36px;
    letter-spacing: 2px;
    
    &::after {
      width: 80px;
    }
  }
`;

interface SectionTitleProps extends Omit<TitleProps, 'order'> {
  children: React.ReactNode;
  className?: string;
}

export const SectionTitle: React.FC<SectionTitleProps> = ({ 
  children, 
  className = '',
  ...props 
}) => {
  return (
    <StyledTitle order={2} className={className} {...props}>
      {children}
    </StyledTitle>
  );
};

// Usage examples
export const SectionTitleExamples = () => (
  <>
    <SectionTitle>Upcoming Classes & Events</SectionTitle>
    <SectionTitle>What Makes Our Community Special</SectionTitle>
  </>
);
```

## Layout Components

### Header Component

**File**: `src/components/layout/Header/Header.tsx`

```typescript
import React, { useEffect, useState } from 'react';
import { AppShell, Container, Group, Burger, Drawer } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { styled } from '@emotion/styled';
import { NavLink } from '../../ui/NavLink/NavLink';
import { Button } from '../../ui/Button/Button';

const StyledHeader = styled(AppShell.Header)<{ scrolled: boolean }>`
  background: rgba(255, 248, 240, 0.95);
  backdrop-filter: blur(10px);
  box-shadow: ${props => props.scrolled 
    ? '0 4px 30px rgba(0,0,0,0.12)' 
    : '0 2px 20px rgba(0,0,0,0.08)'};
  padding: var(--space-sm) 40px;
  border-bottom: 3px solid ${props => props.scrolled 
    ? 'rgba(183, 109, 117, 0.5)' 
    : 'rgba(183, 109, 117, 0.3)'};
  transition: all 0.3s ease;
  
  ${props => props.scrolled && `
    padding: 12px 40px;
  `}
  
  @media (max-width: 768px) {
    padding: var(--space-sm) 20px;
  }
`;

const UtilityBar = styled.div`
  background: var(--color-midnight);
  padding: 12px 40px;
  font-size: 13px;
  color: var(--color-taupe);
  display: flex;
  justify-content: flex-end;
  align-items: center;
  font-family: var(--font-heading);
  text-transform: uppercase;
  letter-spacing: 1px;
  
  @media (max-width: 768px) {
    padding: 10px 20px;
    font-size: 11px;
  }
`;

const UtilityLink = styled.a`
  color: var(--color-taupe);
  text-decoration: none;
  margin-left: var(--space-lg);
  transition: all 0.3s ease;
  position: relative;
  
  &::after {
    content: '';
    position: absolute;
    bottom: -2px;
    left: 50%;
    width: 0;
    height: 1px;
    background: var(--color-rose-gold);
    transform: translateX(-50%);
    transition: width 0.3s ease;
  }
  
  &:hover {
    color: var(--color-rose-gold);
    text-decoration: none;
    
    &::after {
      width: 100%;
    }
  }
  
  &.incident-link {
    color: var(--color-brass);
    font-weight: 600;
  }
  
  @media (max-width: 768px) {
    margin-left: var(--space-md);
    font-size: 10px;
  }
`;

const Logo = styled.a`
  font-family: var(--font-heading);
  font-size: 30px;
  font-weight: 800;
  color: var(--color-burgundy);
  text-decoration: none;
  letter-spacing: -0.5px;
  transition: all 0.3s ease;
  position: relative;
  
  &::after {
    content: '';
    position: absolute;
    bottom: -4px;
    left: 50%;
    transform: translateX(-50%);
    width: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
    transition: width 0.3s ease;
  }
  
  &:hover {
    color: var(--color-burgundy-light);
    text-decoration: none;
    transform: scale(1.02);
    
    &::after {
      width: 100%;
    }
  }
  
  @media (max-width: 768px) {
    font-size: 28px;
  }
`;

const Navigation = styled(Group)`
  @media (max-width: 768px) {
    display: none;
  }
`;

const MobileNavigation = styled.div`
  padding: var(--space-lg);
  
  .nav-item {
    display: block;
    padding: var(--space-md) 0;
    border-bottom: 1px solid var(--color-taupe);
    
    &:last-child {
      border-bottom: none;
    }
  }
`;

interface HeaderProps {
  currentPath?: string;
}

export const Header: React.FC<HeaderProps> = ({ currentPath = '' }) => {
  const [scrolled, setScrolled] = useState(false);
  const [drawerOpened, { toggle: toggleDrawer, close: closeDrawer }] = useDisclosure();
  
  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 100);
    };
    
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);
  
  const navigationItems = [
    { href: '#events', label: 'Events & Classes' },
    { href: '#join', label: 'How to Join' },
    { href: '#resources', label: 'Resources' },
  ];
  
  return (
    <>
      <UtilityBar>
        <UtilityLink href="#" className="incident-link">
          Report an Incident
        </UtilityLink>
        <UtilityLink href="#">Private Lessons</UtilityLink>
        <UtilityLink href="#">Contact</UtilityLink>
      </UtilityBar>
      
      <StyledHeader scrolled={scrolled}>
        <Container size="xl" px={0}>
          <Group justify="space-between" align="center">
            <Logo href="#">WITCH CITY ROPE</Logo>
            
            <Navigation gap="var(--space-xl)" align="center">
              {navigationItems.map((item) => (
                <NavLink
                  key={item.href}
                  href={item.href}
                  active={currentPath === item.href}
                >
                  {item.label}
                </NavLink>
              ))}
              <Button variant="primary" href="#login">
                Login
              </Button>
            </Navigation>
            
            <Burger
              opened={drawerOpened}
              onClick={toggleDrawer}
              size="md"
              color="var(--color-burgundy)"
              style={{ display: 'none' }}
              className="mobile-only"
            />
          </Group>
        </Container>
      </StyledHeader>
      
      <Drawer
        opened={drawerOpened}
        onClose={closeDrawer}
        position="right"
        size="sm"
        title="Navigation"
      >
        <MobileNavigation>
          {navigationItems.map((item) => (
            <NavLink
              key={item.href}
              href={item.href}
              active={currentPath === item.href}
              onClick={closeDrawer}
              className="nav-item"
            >
              {item.label}
            </NavLink>
          ))}
          <div style={{ marginTop: 'var(--space-lg)' }}>
            <Button variant="primary" href="#login" onClick={closeDrawer}>
              Login
            </Button>
          </div>
        </MobileNavigation>
      </Drawer>
    </>
  );
};
```

## Implementation Best Practices

### TypeScript Integration
```typescript
// Always provide proper type definitions
interface ComponentProps {
  required: string;
  optional?: boolean;
  children: React.ReactNode;
}

// Use generic types for flexible components
interface GenericProps<T = any> {
  data: T;
  onSelect: (item: T) => void;
}
```

### Accessibility Requirements
```typescript
// Always include proper ARIA attributes
const AccessibleButton = () => (
  <button
    aria-label="Close dialog"
    aria-describedby="button-description"
    role="button"
    tabIndex={0}
  >
    ×
  </button>
);

// Use semantic HTML
const Navigation = () => (
  <nav aria-label="Main navigation">
    <ul role="list">
      <li role="listitem">
        <a href="#" aria-current="page">Home</a>
      </li>
    </ul>
  </nav>
);
```

### Performance Optimization
```typescript
// Use React.memo for expensive components
export const ExpensiveComponent = React.memo(({ data }) => {
  // Complex rendering logic
});

// Use useMemo for expensive calculations
const SortedData = ({ items }) => {
  const sortedItems = useMemo(
    () => items.sort((a, b) => a.name.localeCompare(b.name)),
    [items]
  );
  
  return <div>{/* Render sorted items */}</div>;
};
```

### Style Organization
```typescript
// Use consistent styling patterns
const StyledComponent = styled.div`
  /* Layout properties first */
  display: flex;
  align-items: center;
  
  /* Spacing */
  padding: var(--space-md);
  margin: var(--space-sm) 0;
  
  /* Typography */
  font-family: var(--font-heading);
  font-size: 16px;
  
  /* Colors */
  color: var(--color-charcoal);
  background: var(--color-ivory);
  
  /* Borders and shadows */
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  
  /* Transitions */
  transition: all 0.3s ease;
  
  /* Responsive */
  @media (max-width: 768px) {
    font-size: 14px;
    padding: var(--space-sm);
  }
`;
```

## Testing Components

### Unit Testing Example
```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { Button } from './Button';

describe('Button Component', () => {
  it('renders with correct text', () => {
    render(<Button>Click me</Button>);
    expect(screen.getByText('Click me')).toBeInTheDocument();
  });
  
  it('handles click events', () => {
    const handleClick = jest.fn();
    render(<Button onClick={handleClick}>Click me</Button>);
    fireEvent.click(screen.getByText('Click me'));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });
  
  it('applies correct variant styles', () => {
    render(<Button variant="electric">Electric Button</Button>);
    const button = screen.getByText('Electric Button');
    expect(button).toHaveClass('btn-electric');
  });
});
```

### Visual Testing with Storybook
```typescript
// Button.stories.tsx
import type { Meta, StoryObj } from '@storybook/react';
import { Button } from './Button';

const meta: Meta<typeof Button> = {
  title: 'UI/Button',
  component: Button,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Primary: Story = {
  args: {
    variant: 'primary',
    children: 'Primary Button',
  },
};

export const Electric: Story = {
  args: {
    variant: 'electric',
    children: 'Electric Button',
  },
};

export const Large: Story = {
  args: {
    variant: 'primary',
    large: true,
    children: 'Large Button',
  },
};
```

## Component Documentation Standards

Each component should include:
1. **TypeScript interface** with all props documented
2. **Usage examples** with common patterns
3. **Accessibility notes** for screen readers
4. **Mobile considerations** and responsive behavior
5. **Performance notes** for optimization
6. **Testing examples** for quality assurance

## Next Steps

1. **Implement core components**: Start with Button, NavLink, and EventCard
2. **Set up Storybook**: Create visual component library
3. **Add testing**: Implement unit and integration tests
4. **Document patterns**: Create component usage guidelines
5. **Optimize performance**: Add React.memo where needed

---

**Reference**: Study the working template at [homepage-template-v7.html](../current/homepage-template-v7.html) for complete implementation examples and styling details.