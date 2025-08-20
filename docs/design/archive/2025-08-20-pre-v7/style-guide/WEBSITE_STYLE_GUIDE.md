# Witch City Rope Website Style Guide

## Overview
This document defines the standardized visual design system for the Witch City Rope website, based on the enhanced design implemented in landing-page-visual-v2.html and event-list-visual.html.

## Color Palette

### Primary Colors
```css
--color-burgundy: #880124;
--color-burgundy-dark: #660119;
--color-burgundy-light: #AA0130;

--color-plum: #614B79;
--color-plum-dark: #4A3A5C;
--color-plum-light: #7B6296;
```

### Warm Metallics
```css
--color-rose-gold: #B76D75;
--color-copper: #B87333;
--color-brass: #C9A961;
```

### CTA Accent Colors
```css
--color-electric: #9D4EDD;
--color-electric-dark: #7B2CBF;
--color-amber: #FFBF00;
--color-amber-dark: #E6AC00;
--color-amber-light: #FFD633;
```

### Neutral Colors
```css
--color-midnight: #1A1A2E;
--color-charcoal: #2B2B2B;
--color-gray-dark: #2D2D2D;
--color-gray: #666666;
--color-gray-light: #CCCCCC;
--color-stone: #8B8680;
--color-taupe: #B8B0A8;
--color-ivory: #FFF8F0;
--color-cream: #FAF6F2;
--color-off-white: #F5F5F5;
```

### Supporting Tones
```css
--color-dusty-rose: #D4A5A5;
```

### Status Colors
```css
--color-success: #228B22;
--color-warning: #DAA520;
--color-error: #DC143C;
--color-info: #614B79;

--color-success-light: #E6F7F1;
--color-warning-light: #FFF4CC;
--color-error-light: #FFE5EC;
--color-info-light: #F0EDFF;
```

## Typography

### Font Families
```css
--font-display: 'Bodoni Moda', serif;
--font-heading: 'Montserrat', sans-serif;
--font-body: 'Source Sans 3', sans-serif;
--font-accent: 'Satisfy', cursive;
```

### Font Import
```html
<link href="https://fonts.googleapis.com/css2?family=Bodoni+Moda:opsz,wght@6..96,400;6..96,700;6..96,900&family=Montserrat:wght@400;500;600;700;800&family=Source+Sans+3:wght@300;400;600&family=Satisfy&display=swap" rel="stylesheet">
```

### Font Sizes
- Base body text: 18px
- Hero heading: 48px
- Section headings: 36px
- Card titles: 22px
- Navigation: 15px
- Small text: 14px

## Spacing Scale
```css
--space-xs: 8px;
--space-sm: 16px;
--space-md: 24px;
--space-lg: 32px;
--space-xl: 40px;
--space-2xl: 48px;
--space-3xl: 64px;
```

## Navigation Structure

### Utility Bar
Located at the very top of the page with dark background.

**HTML Structure:**
```html
<!-- Utility Bar -->
<div class="utility-bar">
    <a href="#" class="incident-link">Report an Incident</a>
    <a href="#">Private Lessons</a>
    <a href="#">Contact</a>
</div>
```

**CSS Styles:**
```css
.utility-bar {
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
}

.utility-bar a {
    color: var(--color-taupe);
    text-decoration: none;
    margin-left: var(--space-lg);
    transition: all 0.3s ease;
    position: relative;
}

.utility-bar a::after {
    content: '';
    position: absolute;
    bottom: -2px;
    left: 0;
    width: 0;
    height: 1px;
    background: var(--color-rose-gold);
    transition: width 0.3s ease;
}

.utility-bar a:hover {
    color: var(--color-rose-gold);
}

.utility-bar a:hover::after {
    width: 100%;
}

.utility-bar a.incident-link {
    color: var(--color-brass);
    font-weight: 600;
}
```

### Main Header
Transparent background with backdrop filter effect.

**HTML Structure:**
```html
<!-- Header -->
<header class="header" id="header">
    <a href="landing-page-visual-v2.html" class="logo">WITCH CITY ROPE</a>
    <nav class="nav">
        <a href="event-list-visual.html" class="nav-item">Events & Classes</a>
        <a href="vetting-application.html" class="nav-item">How to Join</a>
        <a href="#" class="nav-item">Resources</a>
        <a href="auth-login-register-visual.html" class="btn btn-primary">Login</a>
    </nav>
    <button class="mobile-menu-toggle">
        <span></span>
        <span></span>
        <span></span>
    </button>
</header>
```

**CSS Styles:**
```css
.header {
    background: rgba(255, 248, 240, 0.95);
    backdrop-filter: blur(10px);
    box-shadow: 0 2px 20px rgba(0,0,0,0.08);
    padding: var(--space-sm) 40px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    position: sticky;
    top: 0;
    z-index: 100;
    transition: all 0.3s ease;
}

.header.scrolled {
    padding: 12px 40px;
    box-shadow: 0 4px 30px rgba(0,0,0,0.12);
}

.logo {
    font-family: var(--font-heading);
    font-size: 30px;
    font-weight: 800;
    color: var(--color-burgundy);
    text-decoration: none;
    letter-spacing: -0.5px;
    transition: all 0.3s ease;
}

.logo:hover {
    color: var(--color-burgundy-light);
    transform: scale(1.02);
}

.nav {
    display: flex;
    gap: var(--space-xl);
    align-items: center;
}

.nav-item {
    color: var(--color-charcoal);
    text-decoration: none;
    font-family: var(--font-heading);
    font-weight: 500;
    font-size: 15px;
    text-transform: uppercase;
    letter-spacing: 1px;
    transition: all 0.3s ease;
    position: relative;
}

.nav-item::before {
    content: '';
    position: absolute;
    bottom: -4px;
    left: 50%;
    transform: translateX(-50%);
    width: 0;
    height: 2px;
    background: var(--color-burgundy);
    transition: width 0.3s ease;
}

.nav-item:hover {
    color: var(--color-burgundy);
}

.nav-item:hover::before {
    width: 100%;
}
```

### Mobile Menu Toggle
```css
.mobile-menu-toggle {
    display: none;
    flex-direction: column;
    justify-content: space-between;
    width: 30px;
    height: 24px;
    background: none;
    border: none;
    cursor: pointer;
    padding: 0;
}

.mobile-menu-toggle span {
    display: block;
    width: 100%;
    height: 3px;
    background: var(--color-burgundy);
    border-radius: 2px;
    transition: all 0.3s ease;
}

@media (max-width: 768px) {
    .mobile-menu-toggle {
        display: flex;
    }
    
    .nav {
        display: none;
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: rgba(255, 248, 240, 0.98);
        flex-direction: column;
        padding: var(--space-md);
        gap: var(--space-md);
        box-shadow: 0 10px 30px rgba(0,0,0,0.1);
    }
    
    .nav.active {
        display: flex;
    }
}
```

## Button Styles

### Primary Button (CTA)
```css
.btn {
    padding: 14px 32px;
    border-radius: 4px;
    text-decoration: none;
    font-family: var(--font-heading);
    font-weight: 600;
    font-size: 14px;
    text-transform: uppercase;
    letter-spacing: 1.5px;
    transition: all 0.4s ease;
    display: inline-block;
    cursor: pointer;
    border: none;
    position: relative;
    overflow: hidden;
}

.btn-primary {
    background: linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%);
    color: var(--color-midnight);
    box-shadow: 0 4px 15px rgba(255, 191, 0, 0.4);
    font-weight: 700;
}

.btn-primary::before {
    content: '';
    position: absolute;
    top: 0;
    left: -100%;
    width: 100%;
    height: 100%;
    background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
    transition: left 0.5s ease;
}

.btn-primary:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 25px rgba(255, 191, 0, 0.5);
    background: linear-gradient(135deg, var(--color-amber-dark) 0%, var(--color-amber) 100%);
}

.btn-primary:hover::before {
    left: 100%;
}
```

### Secondary Button
```css
.btn-secondary {
    background: transparent;
    color: var(--color-burgundy);
    border: 2px solid var(--color-burgundy);
    position: relative;
    z-index: 1;
}

.btn-secondary::before {
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

.btn-secondary:hover {
    color: var(--color-ivory);
}

.btn-secondary:hover::before {
    width: 100%;
}
```

## Page Background
```css
body {
    font-family: var(--font-body);
    font-size: 18px;
    line-height: 1.6;
    color: var(--color-charcoal);
    background-color: var(--color-cream);
    overflow-x: hidden;
}
```

## Common Components

### Hero Sections
- Gradient background: `linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)`
- Reduced padding for event pages: `calc(var(--space-3xl) * 0.75)`
- Full padding for landing: `var(--space-3xl)`

### Cards
- Background: white or `var(--color-ivory)`
- Border radius: 8px
- Box shadow: `0 4px 12px rgba(0,0,0,0.08)`
- Hover: Transform and enhanced shadow

### Focus States
- Outline: `3px solid rgba(97, 75, 121, 0.1)`
- Border color: `var(--color-plum)`

## JavaScript Enhancements

### Header Scroll Effect
```javascript
window.addEventListener('scroll', function() {
    const header = document.getElementById('header');
    if (window.scrollY > 100) {
        header.classList.add('scrolled');
    } else {
        header.classList.remove('scrolled');
    }
});
```

### Mobile Menu Toggle
```javascript
document.querySelector('.mobile-menu-toggle').addEventListener('click', function() {
    document.querySelector('.nav').classList.toggle('active');
});
```