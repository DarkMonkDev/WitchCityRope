# WitchCityRope User Menu Wireframes & Visual Specifications

## Desktop Wireframes

### 1. Desktop - Unauthenticated State
```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ ┌─────────────────────────────────────────────────────────────────────────────┐ │
│ │                     Report an Incident  Private Lessons  Contact              │ │
│ └─────────────────────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────────────────────┐ │
│ │                                                                               │ │
│ │  WITCH CITY ROPE    Events & Classes   How To Join   Resources   [ LOGIN ]   │ │
│ │                                                                               │ │
│ └─────────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘

Button Details:
┌─────────────┐
│   LOGIN     │  <- Amber gradient button (#FFBF00 to #FF8C00)
└─────────────┘     14px Montserrat, uppercase, letter-spacing: 1.5px
                    Padding: 12px 28px
                    Border-radius: 4px
                    Box-shadow: 0 4px 15px rgba(255, 191, 0, 0.4)
```

### 2. Desktop - Authenticated State (Normal)
```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ ┌─────────────────────────────────────────────────────────────────────────────┐ │
│ │                     Report an Incident  Private Lessons  Contact              │ │
│ └─────────────────────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────────────────────┐ │
│ │                                                                               │ │
│ │  WITCH CITY ROPE    Events & Classes   How To Join   My Dashboard            │ │
│ │                                                                               │ │
│ │                     Resources   [●] RopeArtist ▼                              │ │
│ │                                 └─┘             └─ chevron-down icon          │ │
│ │                                  │                                            │ │
│ │                                  └─ 28px circular avatar                      │ │
│ └─────────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘

User Menu Button:
┌────────────────────────┐
│ [●] RopeArtist ▼      │  <- Transparent background
└────────────────────────┘     Padding: 8px
                               Gap: 8px between elements
                               Border-radius: 8px on hover
```

### 3. Desktop - Dropdown Menu Expanded
```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ ┌─────────────────────────────────────────────────────────────────────────────┐ │
│ │                     Report an Incident  Private Lessons  Contact              │ │
│ └─────────────────────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────────────────────┐ │
│ │                                                                               │ │
│ │  WITCH CITY ROPE    Events & Classes   How To Join   My Dashboard            │ │
│ │                                                                               │ │
│ │                     Resources   [●] RopeArtist ▼                              │ │
│ │                                 ┌─────────────────────┐                       │ │
│ │                                 │ 👤 My Profile       │ <- White background   │ │
│ │                                 │ 🎫 My Tickets       │    Border-radius: 8px │ │
│ │                                 │ ⚙️  Settings         │    Shadow: lg         │ │
│ │                                 │ ─────────────────── │ <- Divider            │ │
│ │                                 │ 🛡️  Admin Panel     │ <- Only if admin     │ │
│ │                                 │ ─────────────────── │                       │ │
│ │                                 │ 🚪 Logout           │                       │ │
│ │                                 └─────────────────────┘                       │ │
│ └─────────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────┘

Dropdown Details:
- Position: absolute, top: 100%, right: 0
- Min-width: 200px
- Margin-top: 8px
- Background: white
- Box-shadow: 0 10px 25px rgba(0,0,0,0.1), 0 5px 10px rgba(0,0,0,0.05)
- Animation: fade in + slide down (opacity 0->1, translateY -10px->0)
```

### 4. Desktop - Hover States
```
Login Button Hover:
┌─────────────┐
│   LOGIN     │  <- Transform: translateY(-2px)
└─────────────┘     Box-shadow: 0 6px 25px rgba(255, 191, 0, 0.5)
     ▲              Background: gradient reversed (#FF8C00 to #FFBF00)
     │
  Elevated 2px

User Menu Button Hover:
┌────────────────────────┐
│ [●] RopeArtist ▼      │  <- Background: #FFF8F0 (cream)
└────────────────────────┘     Transition: all 0.3s ease

Dropdown Item Hover:
┌─────────────────────┐
│ 👤 My Profile       │  <- Background: #FFF8F0 (cream)
└─────────────────────┘     Color: #8B1538 (burgundy)
                            Transition: all 0.3s ease
```

## Mobile Wireframes

### 5. Mobile - Menu with Login Button
```
┌─────────────────────────┐
│ ┌─────────────────────┐ │
│ │ WITCH CITY ROPE   ☰ │ │  <- Header with hamburger
│ └─────────────────────┘ │
│                         │
│ Mobile Menu (Slide-in)  │
│ ┌─────────────────────┐ │
│ │ WITCH CITY ROPE   × │ │  <- Menu header with close
│ ├─────────────────────┤ │
│ │ Events & Classes    │ │
│ ├─────────────────────┤ │
│ │ How To Join         │ │
│ ├─────────────────────┤ │
│ │ Resources           │ │
│ ├─────────────────────┤ │
│ │ Report an Incident  │ │  <- Brass color (#B8860B)
│ ├─────────────────────┤ │
│ │ Private Lessons     │ │
│ ├─────────────────────┤ │
│ │ Contact             │ │
│ ├─────────────────────┤ │
│ │                     │ │
│ │ ┌─────────────────┐ │ │
│ │ │     LOGIN       │ │ │  <- Full width amber button
│ │ └─────────────────┘ │ │
│ │                     │ │
│ └─────────────────────┘ │
└─────────────────────────┘

Mobile Menu Details:
- Width: 80%, max-width: 300px
- Background: #FFF8F0 (ivory)
- Slide in from right
- Box-shadow: -4px 0 20px rgba(0,0,0,0.1)
```

### 6. Mobile - Menu with User Info
```
┌─────────────────────────┐
│ ┌─────────────────────┐ │
│ │ WITCH CITY ROPE   ☰ │ │
│ └─────────────────────┘ │
│                         │
│ Mobile Menu (Slide-in)  │
│ ┌─────────────────────┐ │
│ │ WITCH CITY ROPE   × │ │
│ ├─────────────────────┤ │
│ │ ┌───┬─────────────┐ │ │  <- User info section
│ │ │ ● │ RopeArtist  │ │ │     48px avatar
│ │ │   │ user@...com │ │ │     Name + email
│ │ └───┴─────────────┘ │ │
│ ├─────────────────────┤ │
│ │ Events & Classes    │ │
│ ├─────────────────────┤ │
│ │ How To Join         │ │
│ ├─────────────────────┤ │
│ │ My Dashboard        │ │  <- Additional when logged in
│ ├─────────────────────┤ │
│ │ My Tickets          │ │
│ ├─────────────────────┤ │
│ │ Resources           │ │
│ ├─────────────────────┤ │
│ │ My Profile          │ │
│ ├─────────────────────┤ │
│ │ Settings            │ │
│ ├─────────────────────┤ │
│ │ Admin Panel         │ │  <- Only if admin (burgundy)
│ ├─────────────────────┤ │
│ │ Report an Incident  │ │
│ ├─────────────────────┤ │
│ │ Private Lessons     │ │
│ ├─────────────────────┤ │
│ │ Contact             │ │
│ ├─────────────────────┤ │
│ │                     │ │
│ │ ┌─────────────────┐ │ │
│ │ │     LOGOUT      │ │ │  <- Secondary style button
│ │ └─────────────────┘ │ │
│ │                     │ │
│ └─────────────────────┘ │
└─────────────────────────┘
```

## Visual Specifications

### Colors
```
Primary CTA (Login/Logout):
- Base: #FFBF00 (Amber)
- Gradient End: #FF8C00 (Dark Amber)
- Text: #1A1A2E (Midnight)
- Shadow: rgba(255, 191, 0, 0.4)
- Hover Shadow: rgba(255, 191, 0, 0.5)

User Interface:
- Background: #FFF8F0 (Ivory/Cream)
- Text Primary: #2C2C2C (Charcoal)
- Text Secondary: #6B6B6B (Stone)
- Hover State: #FFF8F0 (Cream)
- Hover Text: #8B1538 (Burgundy)
- Divider: #FFF8F0 (Cream)

Special States:
- Admin: #8B1538 (Burgundy)
- Incident: #B8860B (Brass)
```

### Typography
```
Buttons:
- Font: Montserrat
- Size: 14px
- Weight: 600
- Transform: uppercase
- Letter-spacing: 1.5px

User Name:
- Font: System/Body font
- Size: 14px
- Weight: 400
- Color: #2C2C2C

Dropdown Items:
- Font: System/Body font
- Size: 14px
- Weight: 400
- Icon Size: 16px
- Icon Gap: 8px

Mobile Menu:
- Item Font: Heading font
- Item Size: 16px
- Item Weight: 500
- User Name: 16px, weight 600
- User Email: 14px, weight 400
```

### Spacing & Layout
```
Desktop Header:
- Padding: 16px 40px (normal), 12px 40px (scrolled)
- Nav Gap: 32px between items
- User Menu Gap: 32px from last nav item

Login Button:
- Padding: 12px 28px
- Border-radius: 4px

User Menu Button:
- Padding: 8px
- Avatar Size: 28px
- Gap: 8px (between avatar, name, chevron)
- Border-radius: 8px (hover state)

Dropdown:
- Margin-top: 8px from button
- Min-width: 200px
- Item Padding: 12px 16px
- Border-radius: 8px
- Divider: 1px height, 4px margin

Mobile:
- Menu Width: 80%, max 300px
- Header Padding: 16px
- Item Padding: 12px 0
- User Info Padding: 16px 0
- Avatar Size: 48px
- Button Margin: 24px 0
```

### Animations
```
Dropdown Open:
- Duration: 300ms
- Easing: ease
- Opacity: 0 -> 1
- Transform: translateY(-10px) -> translateY(0)

Button Hover:
- Duration: 400ms
- Easing: ease
- Transform: translateY(0) -> translateY(-2px)
- Shadow: expand and intensify

Mobile Menu:
- Duration: 300ms
- Easing: ease
- Slide: right -100% -> right 0

Overlay Fade:
- Duration: 300ms
- Opacity: 0 -> 0.5
```

### Shadows
```
Login Button:
- Default: 0 4px 15px rgba(255, 191, 0, 0.4)
- Hover: 0 6px 25px rgba(255, 191, 0, 0.5)

Dropdown:
- Box-shadow: 0 10px 25px rgba(0,0,0,0.1), 0 5px 10px rgba(0,0,0,0.05)

Mobile Menu:
- Box-shadow: -4px 0 20px rgba(0,0,0,0.1)

Header (scrolled):
- Box-shadow: 0 4px 30px rgba(0,0,0,0.12)
```

## Implementation Notes

1. **Accessibility**:
   - All interactive elements must have proper ARIA labels
   - Dropdown should be keyboard navigable
   - Focus states should be clearly visible
   - Mobile menu should trap focus when open

2. **Responsive Breakpoints**:
   - Desktop: > 768px
   - Mobile: ≤ 768px

3. **State Management**:
   - Track user authentication state
   - Manage dropdown open/close state
   - Handle click-outside to close dropdown
   - Prevent body scroll when mobile menu is open

4. **Performance**:
   - Use CSS transitions instead of JavaScript animations
   - Lazy-load user avatar images
   - Debounce scroll events for header state

5. **Cross-browser Compatibility**:
   - Test gradient backgrounds in all browsers
   - Ensure proper flexbox fallbacks
   - Verify shadow rendering consistency