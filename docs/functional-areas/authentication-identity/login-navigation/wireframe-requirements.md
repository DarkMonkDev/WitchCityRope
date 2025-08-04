# Wireframe Requirements for Login Navigation

## Overview
Based on analysis of the wireframe files, the navigation should have a premium, luxurious aesthetic with clear role-based menu items and sophisticated visual design.

## Expected Navigation Structure

### Unauthenticated Users
- **Primary Nav**: Events & Classes, How To Join, Resources
- **Right Side**: Login button (styled as premium button)

### Authenticated Users
- **Primary Nav**: Dashboard, My Events, Edit Profile, Settings
- **Right Side**: User avatar with dropdown menu

### Admin Users
- All authenticated user items plus:
- Admin Panel link in dropdown or primary nav

## Visual Design Requirements

### Typography
- **Logo**: Montserrat, 30px, weight 800, uppercase
- **Nav Links**: Montserrat, 15px, weight 500, uppercase, 1px letter spacing
- **Body Text**: Source Sans Pro

### Color Palette
- **Primary**: Burgundy (#880124)
- **Secondary**: Plum (#8B4A6C)
- **Accent**: Rose Gold (#E8B4B8)
- **Background**: Ivory (#FFF8F0)
- **Dark**: Midnight (#1A1A2E)
- **Light**: Taupe (#D4C5B9)

### Navigation Styling
- **Header**: Sticky with backdrop blur, semi-transparent ivory background
- **Links**: Uppercase with hover underline animation from center
- **User Avatar**: 40x40px circular with gradient background
- **Dropdown**: Elegant dropdown with shadow and smooth animation

### User Menu Items (Dropdown)
1. Profile Settings
2. Security Settings  
3. Membership Settings
4. Emergency Contacts
5. Sign Out

### Utility Bar
- Dark background (midnight color)
- Contains: Report an Incident (brass), Private Lessons, Contact
- Right-aligned with rose-gold hover effects

## Current Implementation Gaps

### Visual Design
- [ ] Missing utility bar component
- [ ] Incorrect typography (not using Montserrat)
- [ ] Wrong color scheme
- [ ] Missing gradient effects on avatars
- [ ] No hover animations

### Navigation Structure  
- [ ] Shows "My Tickets" instead of "My Events"
- [ ] "Edit Profile" in dropdown instead of main nav
- [ ] Missing some dropdown menu items
- [ ] No visual hierarchy

### Functionality
- [ ] Navigation doesn't update after login
- [ ] User avatar/name not displayed
- [ ] Role-based items not working

## Implementation Priority

1. **Fix Core Functionality** - Make navigation update after login
2. **Update Menu Structure** - Match wireframe navigation items
3. **Apply Visual Design** - Implement colors, typography, and effects
4. **Add Animations** - Hover states and transitions
5. **Test Responsiveness** - Ensure mobile menu works correctly