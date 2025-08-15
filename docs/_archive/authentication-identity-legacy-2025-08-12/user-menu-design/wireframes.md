# User Menu Wireframes and Visual Design

## Desktop Wireframes

### 1. Unauthenticated State
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ WITCH CITY ROPE    Events & Classes   How To Join   Resources      [LOGIN]  │
│                                                                              │
│                                                          ┌──────────────┐    │
│                                                          │    LOGIN     │    │
│                                                          │              │    │
│                                                          └──────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘

Button Details:
- Background: Linear gradient #FFBF00 → #FF8C00
- Text: "LOGIN" (uppercase)
- Padding: 12px 28px
- Shadow: 0 4px 15px rgba(255, 191, 0, 0.4)
- Hover: Lift 2px up, shadow intensifies
```

### 2. Authenticated State - Closed
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ WITCH CITY ROPE    Events & Classes   How To Join   My Dashboard           │
│                                                      Resources   [●] Jane ▼ │
│                                                                 └─────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘

User Menu Button:
- Avatar: 28px circle
- Name: Scene name (e.g., "RopeKitten")
- Icon: Chevron down (fa-chevron-down)
- Hover: Cream background (#FAF6F2)
```

### 3. Authenticated State - Dropdown Open
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ WITCH CITY ROPE    Events & Classes   How To Join   My Dashboard           │
│                                                      Resources   [●] Jane ▼ │
│                                                                 ┌───────────┐│
│                                                                 │ My Profile││
│                                                                 │ My Tickets││
│                                                                 │ Settings  ││
│                                                                 │───────────││
│                                                                 │Admin Panel││
│                                                                 │───────────││
│                                                                 │ Logout    ││
│                                                                 └───────────┘│
└─────────────────────────────────────────────────────────────────────────────┘

Dropdown Details:
- Width: 200px minimum
- Background: White
- Shadow: 0 10px 40px rgba(0,0,0,0.1)
- Border radius: 8px
- Item padding: 12px 16px
- Icons: Font Awesome (fa-user, fa-ticket-alt, fa-cog, fa-shield-alt, fa-sign-out-alt)
```

### 4. Hover States
```
Normal State:          Hover State:
┌──────────┐          ┌──────────┐
│ My Profile│          │ My Profile│ ← Background: #FAF6F2
└──────────┘          └──────────┘    Color: #880124

LOGIN Button Hover:
┌──────────────┐      ┌──────────────┐
│    LOGIN     │  →   │    LOGIN     │ ← Transform: translateY(-2px)
└──────────────┘      └──────────────┘    Shadow: 0 6px 25px rgba(255, 191, 0, 0.5)
     ↓                      ↑
  Normal                 Lifted
```

## Mobile Wireframes

### 1. Mobile Menu - Unauthenticated
```
┌─────────────────────┐
│ WITCH CITY ROPE   ≡ │ ← Hamburger menu
├─────────────────────┤
│                     │
│ Events & Classes    │
│─────────────────────│
│ How To Join         │
│─────────────────────│
│ Resources           │
│─────────────────────│
│ Report an Incident  │
│─────────────────────│
│ Private Lessons     │
│─────────────────────│
│ Contact             │
│─────────────────────│
│                     │
│   ┌─────────────┐   │
│   │   LOGIN     │   │ ← Full width CTA button
│   └─────────────┘   │
│                     │
└─────────────────────┘
```

### 2. Mobile Menu - Authenticated
```
┌─────────────────────┐
│ WITCH CITY ROPE   × │ ← Close button
├─────────────────────┤
│ ┌─────────────────┐ │
│ │ [●] RopeKitten  │ │ ← User info section
│ │ admin@witch...  │ │
│ └─────────────────┘ │
│─────────────────────│
│ Events & Classes    │
│─────────────────────│
│ How To Join         │
│─────────────────────│
│ My Dashboard        │
│─────────────────────│
│ My Tickets          │
│─────────────────────│
│ Resources           │
│─────────────────────│
│ My Profile          │
│─────────────────────│
│ Settings            │
│─────────────────────│
│ Admin Panel         │ ← Conditional
│─────────────────────│
│ Report an Incident  │
│─────────────────────│
│ Private Lessons     │
│─────────────────────│
│ Contact             │
│─────────────────────│
│   ┌─────────────┐   │
│   │   LOGOUT    │   │ ← Secondary button
│   └─────────────┘   │
└─────────────────────┘
```

## Visual Specifications

### Colors
- **Primary CTA (Login)**: 
  - Background: `linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)`
  - Text: `#1A1A2E` (Midnight)
  - Shadow: `0 4px 15px rgba(255, 191, 0, 0.4)`
  - Hover Shadow: `0 6px 25px rgba(255, 191, 0, 0.5)`

- **User Menu**:
  - Default Background: Transparent
  - Hover Background: `#FAF6F2` (Cream)
  - Text: `#2B2B2B` (Charcoal)
  - Dropdown Background: `#FFFFFF`
  - Dropdown Shadow: `0 10px 40px rgba(0,0,0,0.1)`

- **Special Items**:
  - Admin Panel: `#880124` (Burgundy)
  - Report Incident: `#C9A961` (Brass)

### Typography
- **Login Button**: 
  - Font: Montserrat
  - Size: 14px
  - Weight: 600
  - Transform: Uppercase
  - Letter-spacing: 1.5px

- **User Name**:
  - Font: Source Sans 3
  - Size: 16px
  - Weight: 400

- **Dropdown Items**:
  - Font: Source Sans 3
  - Size: 15px
  - Weight: 400
  - Icon Size: 14px

### Spacing & Dimensions
- **Login Button**: 
  - Padding: 12px 28px
  - Border-radius: 4px
  - Height: 44px

- **User Menu Button**:
  - Padding: 8px
  - Avatar: 28px diameter
  - Gap between elements: 8px

- **Dropdown**:
  - Min-width: 200px
  - Item padding: 12px 16px
  - Border-radius: 8px
  - Gap from trigger: 8px

- **Mobile**:
  - Menu width: 80% (max 300px)
  - User info padding: 16px
  - Nav item padding: 12px 0

### Animations
- **Dropdown Open**: 
  - Duration: 300ms
  - Easing: ease
  - Effect: Fade in + slide down 10px

- **Hover Transitions**:
  - Duration: 300ms
  - Easing: ease
  - Properties: background, color, transform, shadow

- **Login Button Hover**:
  - Transform: translateY(-2px)
  - Duration: 400ms

### Icons (Font Awesome)
- User Profile: `fa-user`
- Tickets: `fa-ticket-alt`
- Settings: `fa-cog`
- Admin: `fa-shield-alt`
- Logout: `fa-sign-out-alt`
- Chevron: `fa-chevron-down`

## Interaction States

### Focus States
- Outline: 2px solid #9D4EDD (Electric Purple)
- Outline-offset: 2px

### Loading States
- Logout: Replace icon with spinner
- Navigation: Maintain button state, show loading indicator

### Error States
- Failed logout: Toast notification
- Network error: Graceful degradation

## Responsive Breakpoints
- Desktop: > 768px
- Mobile: ≤ 768px
- Animations disabled: prefers-reduced-motion

## Accessibility Notes
- All interactive elements have :focus-visible styles
- ARIA labels for icon-only elements
- Proper heading hierarchy maintained
- Color contrast ratio: 4.5:1 minimum
- Touch targets: 44x44px minimum