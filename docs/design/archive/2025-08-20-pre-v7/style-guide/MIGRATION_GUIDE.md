# Style Guide Migration Guide

## Quick Start

1. **Add the design system CSS to your file:**
```html
<link rel="stylesheet" href="../style-guide/design-system.css">
```

2. **Remove inline styles progressively** as you update components
3. **Test each change** in your browser

## Color Migration

### Find & Replace Patterns

```bash
# Primary colors
#8B4513 → var(--color-primary)
#6B3410 → var(--color-primary-hover)
#A0522D → var(--color-primary-light)

# Grays (consolidate duplicates)
#333333 → var(--color-gray-700)
#333 → var(--color-gray-700)
#666666 → var(--color-gray-500)
#666 → var(--color-gray-500)
#999 → var(--color-gray-400)
#ccc → var(--color-gray-300)
#e0e0e0 → var(--color-gray-200)
#f5f5f5 → var(--color-gray-100)
#1a1a1a → var(--color-gray-900)

# Status colors (consolidate variations)
#28a745 → var(--color-success-text)
#2e7d32 → var(--color-success-text)
#4caf50 → var(--color-success-border)

#ffc107 → var(--color-warning-text)
#ff9800 → var(--color-warning-border)
#f57c00 → var(--color-warning-text)
#e65100 → var(--color-warning-text)

#dc3545 → var(--color-error-text)
#d32f2f → var(--color-error-text)
#ff6b6b → var(--color-error-text)
```

### Example Migration

**Before:**
```html
<button style="background-color: #8B4513; color: white; padding: 10px 20px;">
    Register
</button>
```

**After:**
```html
<button class="btn btn-primary">
    Register
</button>
```

## Typography Migration

### Font Size Replacements

```bash
# Exact replacements
font-size: 12px → font-size: var(--text-xs)
font-size: 14px → font-size: var(--text-sm)
font-size: 16px → font-size: var(--text-base)
font-size: 18px → font-size: var(--text-lg)
font-size: 20px → font-size: var(--text-xl)
font-size: 24px → font-size: var(--text-2xl)
font-size: 28px → font-size: var(--text-3xl)
font-size: 32px → font-size: var(--text-4xl)
font-size: 48px → font-size: var(--text-5xl)

# Consolidate similar sizes
font-size: 13px → font-size: var(--text-sm)
font-size: 15px → font-size: var(--text-base)
font-size: 22px → font-size: var(--text-xl)
font-size: 30px → font-size: var(--text-3xl)
```

### Font Weight Standardization

```bash
font-weight: bold → font-weight: var(--font-bold)
font-weight: 700 → font-weight: var(--font-bold)
font-weight: 600 → font-weight: var(--font-semibold)
font-weight: 500 → font-weight: var(--font-medium)
font-weight: 400 → font-weight: var(--font-normal)
font-weight: normal → font-weight: var(--font-normal)
```

## Spacing Migration

### Padding/Margin Replacements

```bash
# Common patterns
padding: 20px → padding: var(--space-5)
padding: 16px → padding: var(--space-4)
padding: 12px → padding: var(--space-3)
padding: 10px → padding: var(--space-3)  # Align to grid
padding: 8px → padding: var(--space-2)

margin-bottom: 20px → margin-bottom: var(--space-5)
margin-bottom: 16px → margin-bottom: var(--space-4)
margin-bottom: 24px → margin-bottom: var(--space-6)
margin-bottom: 30px → margin-bottom: var(--space-8)  # Align to grid

# Complex padding
padding: 10px 20px → padding: var(--space-3) var(--space-5)
padding: 12px 24px → padding: var(--space-3) var(--space-6)
padding: 16px 32px → padding: var(--space-4) var(--space-8)
```

## Component Migration

### Buttons

**Find all button patterns:**
```html
<!-- Pattern 1: Inline styled buttons -->
<button style="background-color: #8B4513; color: white; padding: 10px 20px; border: none; border-radius: 6px;">

<!-- Pattern 2: Class-based buttons -->
<button class="primary-btn">
<button class="btn-primary">
<button class="button-primary">
```

**Replace with:**
```html
<button class="btn btn-primary">Primary Action</button>
<button class="btn btn-secondary">Secondary Action</button>
<button class="btn btn-warning">Warning Action</button>
<button class="btn btn-danger">Danger Action</button>
```

### Form Inputs

**Before:**
```html
<input type="text" style="width: 100%; padding: 12px; border: 1px solid #e0e0e0; border-radius: 6px;">
```

**After:**
```html
<input type="text" class="form-input" placeholder="Enter text">
```

### Cards

**Before:**
```html
<div style="background: white; padding: 20px; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
    <h3 style="font-size: 20px; margin-bottom: 16px;">Card Title</h3>
    <p>Card content</p>
</div>
```

**After:**
```html
<div class="card">
    <h3 class="card-title">Card Title</h3>
    <p>Card content</p>
</div>
```

## File-by-File Migration Order

### Phase 1: High Priority (Most Inconsistent)
1. **landing.html**
   - Uses most custom colors
   - Mixed button styles
   - Custom spacing values

2. **auth-login-register.html**
   - Missing navigation
   - Inconsistent form styling
   - Custom button variants

3. **user-dashboard.html**
   - Multiple warning color variants
   - Mixed component styles

### Phase 2: Event Pages
4. **event-list.html**
5. **event-detail.html** 
6. **member-my-tickets.html**

### Phase 3: Admin Pages
7. **admin-events.html**
8. **admin-vetting-queue.html**
9. **admin-vetting-review.html**

### Phase 4: Supporting Pages
10. Error pages (404, 403, 500)
11. Profile/Settings pages
12. Remaining auth pages

## Validation Checklist

After migrating each file, verify:

- [ ] All colors use CSS variables
- [ ] No hardcoded hex colors remain
- [ ] Typography uses the scale variables
- [ ] Spacing follows 4px grid (mostly)
- [ ] Buttons use consistent classes
- [ ] Forms have consistent styling
- [ ] Interactive elements have focus states
- [ ] Page renders correctly in browser

## Common Pitfalls

1. **Don't forget hover states** - The design system includes hover variants
2. **Preserve responsive behavior** - Test mobile views after changes
3. **Keep accessibility** - Ensure focus states work
4. **Test form validation** - Error states should still display

## VS Code Tips

### Multi-cursor editing:
1. Select a color value (e.g., `#8B4513`)
2. Press `Ctrl+D` (Windows) or `Cmd+D` (Mac) to select next occurrence
3. Keep pressing to select all occurrences
4. Type the replacement `var(--color-primary)`

### Find & Replace with Regex:
```
Find: padding:\s*(\d+)px\s+(\d+)px
Replace: padding: var(--space-$1) var(--space-$2)
```

## Testing Your Changes

1. Open the HTML file in a browser
2. Check responsive behavior (F12 → Device mode)
3. Test all interactive states (hover, focus, active)
4. Verify forms still work
5. Check against the component showcase for consistency