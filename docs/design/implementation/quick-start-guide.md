# v7 Design System Quick Start Guide

**Authority**: This guide is based on the approved Final Design v7 template
**Target**: React developers using Mantine v7
**Last Updated**: 2025-08-20

## Getting Started in 5 Minutes

### 1. Import Design Tokens
```typescript
// src/styles/design-tokens.ts
export const tokens = {
  colors: {
    burgundy: '#880124',
    burgundyDark: '#660018',
    burgundyLight: '#9F1D35',
    roseGold: '#B76D75',
    copper: '#B87333',
    brass: '#C9A961',
    electric: '#9D4EDD',
    electricDark: '#7B2CBF',
    amber: '#FFBF00',
    amberDark: '#FF8C00',
    // ... rest of color palette
  },
  fonts: {
    display: "'Bodoni Moda', serif",
    heading: "'Montserrat', sans-serif",
    body: "'Source Sans 3', sans-serif",
    accent: "'Satisfy', cursive",
  },
  spacing: {
    xs: '8px',
    sm: '16px',
    md: '24px',
    lg: '32px',
    xl: '40px',
    '2xl': '48px',
    '3xl': '64px',
  },
};
```

### 2. Set Up CSS Variables
```css
/* src/styles/globals.css */
:root {
  /* Colors */
  --color-burgundy: #880124;
  --color-burgundy-dark: #660018;
  --color-burgundy-light: #9F1D35;
  --color-rose-gold: #B76D75;
  --color-electric: #9D4EDD;
  --color-electric-dark: #7B2CBF;
  --color-amber: #FFBF00;
  --color-amber-dark: #FF8C00;
  
  /* Typography */
  --font-display: 'Bodoni Moda', serif;
  --font-heading: 'Montserrat', sans-serif;
  --font-body: 'Source Sans 3', sans-serif;
  --font-accent: 'Satisfy', cursive;
  
  /* Spacing */
  --space-xs: 8px;
  --space-sm: 16px;
  --space-md: 24px;
  --space-lg: 32px;
  --space-xl: 40px;
  --space-2xl: 48px;
  --space-3xl: 64px;
}
```

### 3. Import Google Fonts
```html
<!-- In your HTML head or CSS imports -->
<link href="https://fonts.googleapis.com/css2?family=Bodoni+Moda:opsz,wght@6..96,400;6..96,700;6..96,900&family=Montserrat:wght@400;500;600;700;800&family=Source+Sans+3:wght@300;400;600&family=Satisfy&display=swap" rel="stylesheet">
```

## Essential Components

### Signature Button with Corner Animation
```typescript
// components/ui/Button.tsx
import { Button as MantineButton, ButtonProps } from '@mantine/core';
import { styled } from '@emotion/styled';

const StyledButton = styled(MantineButton)`
  border-radius: 12px 6px 12px 6px;
  transition: all 0.3s ease;
  font-family: var(--font-heading);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  
  &:hover {
    border-radius: 6px 12px 6px 12px;
    transform: none; /* NO vertical movement */
  }
  
  /* Primary Amber Variant */
  &.btn-primary {
    background: linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%);
    color: var(--color-midnight);
    box-shadow: 0 4px 15px rgba(255, 191, 0, 0.4);
    
    &:hover {
      background: linear-gradient(135deg, var(--color-amber-dark) 0%, var(--color-amber) 100%);
      box-shadow: 0 6px 25px rgba(255, 191, 0, 0.5);
    }
  }
  
  /* Electric Purple Variant */
  &.btn-electric {
    background: linear-gradient(135deg, var(--color-electric) 0%, var(--color-electric-dark) 100%);
    color: white;
    box-shadow: 0 4px 15px rgba(157, 78, 221, 0.4);
    
    &:hover {
      background: linear-gradient(135deg, var(--color-electric-dark) 0%, var(--color-electric) 100%);
      box-shadow: 0 6px 25px rgba(157, 78, 221, 0.5);
    }
  }
`;

export const Button = ({ children, variant = 'primary', ...props }: ButtonProps & { variant?: 'primary' | 'electric' }) => {
  return (
    <StyledButton className={`btn-${variant}`} {...props}>
      {children}
    </StyledButton>
  );
};
```

### Navigation with Center-Outward Underline
```typescript
// components/ui/NavLink.tsx
import { styled } from '@emotion/styled';

const StyledNavLink = styled.a`
  color: var(--color-charcoal);
  text-decoration: none;
  font-family: var(--font-heading);
  font-weight: 500;
  font-size: 15px;
  text-transform: uppercase;
  letter-spacing: 1px;
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
    color: var(--color-burgundy);
    
    &::after {
      width: 100%;
    }
  }
`;

export const NavLink = ({ children, href, ...props }) => {
  return (
    <StyledNavLink href={href} {...props}>
      {children}
    </StyledNavLink>
  );
};
```

### Feature Icon with Shape-Shifting
```typescript
// components/ui/FeatureIcon.tsx
import { styled } from '@emotion/styled';

const IconContainer = styled.div`
  width: 100px;
  height: 100px;
  background: linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-rose-gold) 100%);
  border-radius: 50% 20% 50% 20%;
  margin: 0 auto var(--space-md);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 48px;
  box-shadow: 0 10px 30px rgba(136, 1, 36, 0.2);
  position: relative;
  overflow: hidden;
  transition: all 0.5s ease;
  
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
`;

const FeatureContainer = styled.div`
  text-align: center;
  
  &:hover ${IconContainer} {
    border-radius: 20% 50% 20% 50%;
    transform: rotate(5deg) scale(1.1);
    
    &::before {
      opacity: 1;
    }
  }
`;

export const FeatureIcon = ({ icon, title, description }) => {
  return (
    <FeatureContainer>
      <IconContainer>
        {icon}
      </IconContainer>
      <h3 style={{ 
        fontFamily: 'var(--font-heading)', 
        fontSize: '24px',
        color: 'var(--color-burgundy)',
        textTransform: 'uppercase',
        letterSpacing: '1px'
      }}>
        {title}
      </h3>
      <p style={{ 
        color: 'var(--color-charcoal)', 
        lineHeight: '1.7' 
      }}>
        {description}
      </p>
    </FeatureContainer>
  );
};
```

## Page Layout Pattern

### Basic Page Structure
```typescript
// components/layout/PageLayout.tsx
import { Box, Container } from '@mantine/core';

export const PageLayout = ({ children }) => {
  return (
    <Box style={{ 
      backgroundColor: 'var(--color-cream)',
      minHeight: '100vh',
      position: 'relative'
    }}>
      {/* Subtle rope pattern background */}
      <Box 
        style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          opacity: 0.03,
          pointerEvents: 'none',
          backgroundImage: `url("data:image/svg+xml,%3Csvg width='200' height='200' viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M40,40 L80,80 L40,120 L80,160' stroke='%23880124' stroke-width='1' fill='none' opacity='0.5'/%3E%3C/svg%3E")`,
          backgroundSize: '200px 200px',
          zIndex: -1
        }} 
      />
      
      <Container size="xl" px={40}>
        {children}
      </Container>
    </Box>
  );
};
```

## Common Patterns

### Section Titles
```typescript
const SectionTitle = ({ children }) => (
  <h2 style={{
    fontFamily: 'var(--font-heading)',
    fontSize: '48px',
    fontWeight: 800,
    textAlign: 'center',
    color: 'var(--color-burgundy)',
    textTransform: 'uppercase',
    letterSpacing: '3px',
    position: 'relative',
    marginBottom: 'var(--space-xl)'
  }}>
    {children}
    <div style={{
      content: '',
      display: 'block',
      width: '100px',
      height: '2px',
      background: 'linear-gradient(90deg, transparent, var(--color-rose-gold), transparent)',
      margin: 'var(--space-sm) auto 0'
    }} />
  </h2>
);
```

### Event Cards
```typescript
const EventCard = ({ title, date, description, price, status }) => (
  <div style={{
    backgroundColor: 'white',
    borderRadius: '16px',
    overflow: 'hidden',
    boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
    transition: 'all 0.3s ease',
    textDecoration: 'none',
    display: 'block',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: '0 8px 24px rgba(0,0,0,0.1)'
    }
  }}>
    {/* Event image header */}
    <div style={{
      height: '100px',
      background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      padding: 'var(--space-md)'
    }}>
      <h3 style={{
        fontFamily: 'var(--font-heading)',
        fontSize: '22px',
        fontWeight: 700,
        color: 'white',
        textAlign: 'center',
        textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)'
      }}>
        {title}
      </h3>
    </div>
    
    {/* Event content */}
    <div style={{ padding: 'var(--space-lg)' }}>
      <div style={{
        fontFamily: 'var(--font-heading)',
        fontSize: '14px',
        fontWeight: 600,
        color: 'var(--color-burgundy)',
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        marginBottom: 'var(--space-xs)'
      }}>
        {date}
      </div>
      
      <p style={{
        color: 'var(--color-stone)',
        fontSize: '15px',
        lineHeight: '1.6',
        marginBottom: 'var(--space-md)'
      }}>
        {description}
      </p>
      
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        paddingTop: 'var(--space-md)',
        borderTop: '1px solid var(--color-taupe)'
      }}>
        <span style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '18px',
          fontWeight: 700,
          color: 'var(--color-burgundy)'
        }}>
          {price}
        </span>
        <span style={{
          fontSize: '14px',
          fontWeight: 600,
          color: status === 'available' ? 'var(--color-success)' : 
                status === 'limited' ? 'var(--color-warning)' : 'var(--color-error)'
        }}>
          {status}
        </span>
      </div>
    </div>
  </div>
);
```

## Mobile Responsive Patterns

### Responsive Typography
```css
/* Mobile-first approach */
.hero-title {
  font-size: 40px; /* Mobile */
  line-height: 1.1;
}

@media (min-width: 769px) {
  .hero-title {
    font-size: 64px; /* Desktop */
  }
}
```

### Responsive Spacing
```css
.section {
  padding: var(--space-xl) 20px; /* Mobile */
}

@media (min-width: 769px) {
  .section {
    padding: var(--space-2xl) 40px; /* Desktop */
  }
}
```

## DO's and DON'Ts

### ✅ DO
- Use CSS variables for all colors and spacing
- Apply signature animations to appropriate elements
- Maintain asymmetric button corner animations
- Use the center-outward underline for navigation
- Follow the established typography hierarchy
- Keep mobile-first responsive approach

### ❌ DON'T
- Hardcode color values
- Add vertical movement to buttons (no translateY)
- Skip the signature animations
- Use different border-radius patterns
- Override the typography system
- Ignore mobile responsiveness

## Next Steps

1. **Read the full design system**: [design-system-v7.md](../current/design-system-v7.md)
2. **Check animation standards**: [animation-standards.md](./animation-standards.md)
3. **Review component library**: [component-library.md](./component-library.md)
4. **Study the working template**: [homepage-template-v7.html](../current/homepage-template-v7.html)

---

**Questions?** This quick start covers the essentials. Refer to the complete design system documentation for detailed specifications and advanced patterns.