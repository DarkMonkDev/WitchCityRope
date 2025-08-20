# Wireframe Standardization Plan

## Overview
This document outlines the standardization approach for all 22 wireframes before applying visual design. The goal is to create consistent layouts and patterns across all pages.

## Standardized Components

### 1. Navigation Structure

#### Public Pages Navigation
```html
<!-- Utility Bar (all non-admin pages) -->
<div class="utility-bar">
    <a href="#">Report an Incident</a>
    <a href="#">Contact Us</a>
</div>

<!-- Main Navigation -->
<header class="main-nav">
    <a href="/" class="logo">Witch City Rope</a>
    <nav>
        <a href="/events">Events & Classes</a>
        <a href="/join">How to Join</a>
        <a href="/resources">Resources</a>
        <a href="/login" class="btn btn-primary">Login</a>
    </nav>
</header>
```

#### Member Pages Navigation
```html
<!-- Same utility bar -->
<header class="main-nav">
    <a href="/" class="logo">Witch City Rope</a>
    <nav>
        <a href="/dashboard">Dashboard</a>
        <a href="/events">Events</a>
        <a href="/my-events">My Events</a>
        <a href="/profile">Profile</a>
        <div class="user-menu">
            <img src="avatar.jpg" alt="User">
            <span>Username</span>
        </div>
    </nav>
</header>
```

#### Admin Pages Navigation
- Keep existing sidebar pattern
- Add utility bar above sidebar

### 2. Page Layout Structure

#### Standard Page Container
```html
<div class="page-container">
    <!-- Navigation (as above) -->
    
    <main class="main-content">
        <div class="page-header">
            <div class="breadcrumb">[optional breadcrumb]</div>
            <h1 class="page-title">Page Title</h1>
            <div class="page-actions">
                <button class="btn btn-primary">Primary Action</button>
            </div>
        </div>
        
        <div class="content-body">
            <!-- Page specific content -->
        </div>
    </main>
    
    <footer class="site-footer">
        <!-- Standard footer -->
    </footer>
</div>
```

### 3. Standardized Spacing

```css
/* Spacing Scale */
--space-xs: 8px;
--space-sm: 16px;
--space-md: 24px;
--space-lg: 32px;
--space-xl: 48px;

/* Standard Applications */
.page-container { padding: var(--space-lg) var(--space-sm); }
.page-header { margin-bottom: var(--space-lg); }
.content-section { margin-bottom: var(--space-lg); }
.form-group { margin-bottom: var(--space-md); }
.card + .card { margin-top: var(--space-md); }
```

### 4. Typography Standards

```css
/* Heading Sizes */
.page-title { font-size: 28px; }
.section-title { font-size: 24px; }
.card-title { font-size: 20px; }
.subsection-title { font-size: 18px; }
body { font-size: 16px; }
.small-text { font-size: 14px; }
.tiny-text { font-size: 12px; }
```

### 5. Button Standards

```css
/* Button Sizes */
.btn-sm { padding: 8px 16px; font-size: 14px; }
.btn { padding: 10px 20px; font-size: 16px; }
.btn-lg { padding: 16px 32px; font-size: 18px; }

/* Button Placement */
.form-actions { margin-top: var(--space-lg); text-align: right; }
.card-footer { padding: var(--space-sm); border-top: 1px solid #e0e0e0; }
```

### 6. Card Standards

```css
.card {
    background: white;
    border-radius: 12px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    padding: var(--space-md);
}

.card-header {
    margin: calc(var(--space-md) * -1);
    margin-bottom: var(--space-md);
    padding: var(--space-sm) var(--space-md);
    border-bottom: 1px solid #e0e0e0;
}

.card-footer {
    margin: calc(var(--space-md) * -1);
    margin-top: var(--space-md);
    padding: var(--space-sm) var(--space-md);
    border-top: 1px solid #e0e0e0;
}
```

### 7. Form Standards

```css
.form-group {
    margin-bottom: var(--space-md);
}

.form-label {
    display: block;
    margin-bottom: 8px;
    font-weight: 500;
}

.form-input,
.form-select,
.form-textarea {
    width: 100%;
    padding: 10px 12px;
    border: 2px solid #e0e0e0;
    border-radius: 6px;
}
```

## Implementation Priority

### Phase 1: High-Traffic Pages
1. `landing.html` - Public face
2. `auth-login-register.html` - Entry point
3. `user-dashboard.html` - Member home
4. `event-list.html` - Core feature
5. `event-detail.html` - Core feature

### Phase 2: Member Pages
6. `member-my-events.html`
7. `member-profile-settings.html`
8. `member-membership-settings.html`
9. `vetting-application.html`

### Phase 3: Admin Pages
10. `admin-events.html`
11. `admin-vetting-queue.html`
12. `admin-vetting-review.html`
13. `event-checkin.html`

### Phase 4: Supporting Pages
14. `auth-2fa-setup.html`
15. `auth-2fa-entry.html`
16. `auth-password-reset.html`
17. `auth-password-reset-form.html`
18. Error pages (404, 403, 500)

## Specific Fixes by Page

### landing.html
- Add standard navigation (currently custom)
- Standardize button sizes
- Apply consistent spacing scale

### auth-* pages
- Add minimal header (logo + opposite action)
- Center form containers consistently
- Standardize form widths (max-width: 400px)

### user-dashboard.html
- Remove custom card shadows
- Apply standard card spacing
- Standardize alert component

### event-list.html
- Consistent card layout
- Standard filter sidebar width
- Uniform button placement

### member-* pages
- Add utility bar
- Fix header positioning (not fixed)
- Consistent settings navigation

### admin-* pages
- Add utility bar above sidebar
- Standardize table layouts
- Consistent modal patterns

## Next Steps

1. Create a `wireframe-base.css` with these standards
2. Update each HTML file to use consistent classes
3. Remove inline styles in favor of classes
4. Test responsive behavior remains intact
5. Document any page-specific exceptions