# Developer Quick Reference

## Essential Colors
```css
/* Primary */
--color-burgundy: #880124;
--color-amber: #FFBF00;

/* Backgrounds */
--color-cream: #FAF6F2;
--color-ivory: #FFF8F0;

/* Text */
--color-charcoal: #2B2B2B;
--color-smoke: #4A4A4A;
```

## Typography Classes
```css
.font-heading { font-family: 'Montserrat', sans-serif; }
.font-body { font-family: 'Source Sans 3', sans-serif; }
```

## Common Components

### Primary Button
```html
<button class="btn btn-primary">
  Register Now
</button>
```

### Form Input
```html
<div class="form-group">
  <label class="form-label">Scene Name</label>
  <input type="text" class="form-input" placeholder="Enter your scene name">
</div>
```

### Card
```html
<div class="card">
  <div class="card-header">
    <h3 class="card-title">Event Title</h3>
  </div>
  <div class="card-body">
    <p>Event description...</p>
  </div>
</div>
```

## Responsive Breakpoints
- Mobile: < 768px
- Tablet: 768px - 1023px
- Desktop: 1024px+

## Spacing Scale
- xs: 8px
- sm: 16px
- md: 24px
- lg: 32px
- xl: 40px

## Quick Tips
1. Always use CSS variables for colors
2. Mobile-first approach
3. Minimum touch target: 44px
4. Focus outline: 3px burgundy
5. All inputs: 16px font size (prevents iOS zoom)

## File Locations
- Final Style Guide: `/docs/design/FINAL-STYLE-GUIDE.md`
- Design System CSS: `/docs/design/style-guide/design-system-enhanced.css`
- Syncfusion Mappings: `/docs/design/syncfusion-component-mapping.md`
- Responsive Fixes: `/docs/design/responsive-design-issues.md`