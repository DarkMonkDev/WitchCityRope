# Typography & Spacing Analysis Report

## Typography Analysis

### Font Family
**Extremely Consistent** - Single font stack used across all files:
```css
font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
```
Secondary mono font for code/technical content:
```css
font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
```

### Font Sizes - Current Usage (23 unique sizes)

| Size | Usage Count | Primary Use | Keep/Remove |
|------|-------------|-------------|-------------|
| **12px** | 45 | Small labels, badges | ✓ Keep |
| 13px | 5 | Inconsistent small text | ✗ → 12px |
| **14px** | 125 | Form labels, secondary text | ✓ Keep |
| 15px | 4 | Random usage | ✗ → 14px |
| **16px** | 165 | Body text default | ✓ Keep |
| 17px | 2 | One-off | ✗ → 16px |
| **18px** | 85 | Emphasized body | ✓ Keep |
| **20px** | 48 | Small headings | ✓ Keep |
| 22px | 8 | Inconsistent | ✗ → 20px or 24px |
| **24px** | 72 | Section headings | ✓ Keep |
| 26px | 3 | Random | ✗ → 24px |
| **28px** | 35 | Page subheadings | ✓ Keep |
| 30px | 12 | Inconsistent | ✗ → 28px or 32px |
| **32px** | 28 | Page titles | ✓ Keep |
| 36px | 4 | One-off | ✗ → 32px |
| 40px | 6 | Special headers | ✗ → 48px |
| **48px** | 15 | Hero text | ✓ Keep |
| 64px | 3 | Error pages | ✓ Keep (special) |
| 80px | 2 | Error pages mobile | Keep for errors |
| 120px | 3 | Error code display | Keep for errors |

### Font Weight Patterns

| Weight | Usage | Standardization Needed |
|--------|-------|------------------------|
| 400 (normal) | Default | Consistent |
| 500 (medium) | 85 instances | Consistent |
| 600 (semibold) | 120 instances | Consistent |
| 700 (bold) | 95 instances | Some use "bold" keyword |
| 900 | 5 instances | Error pages only |

**Issue**: Mix of numeric (700) and keyword (bold) values

### Line Height Usage

| Value | Usage Count | Use Case |
|-------|-------------|----------|
| 1 | 15 | Buttons, compact |
| 1.2 | 25 | Headings |
| 1.4 | 8 | Inconsistent |
| 1.5 | 45 | Body text variant |
| 1.6 | 85 | Body text default |
| 1.8 | 12 | Relaxed spacing |
| 2 | 5 | Special cases |

## Spacing Analysis

### Confirmed Base Unit: 4px

The analysis confirms a 4px base unit is used throughout, though not always consistently.

### Padding Usage Frequency

| Value | Count | Standard | Notes |
|-------|-------|----------|-------|
| 0/0px | 45 | ✓ | Consolidate format |
| 2px | 8 | ⚠️ | Consider 4px |
| 4px | 22 | ✓ | 1x base |
| 6px | 15 | ⚠️ | Not on grid |
| 8px | 85 | ✓ | 2x base |
| 10px | 35 | ⚠️ | Should be 8px or 12px |
| 12px | 125 | ✓ | 3x base |
| 14px | 18 | ⚠️ | Not on grid |
| 16px | 145 | ✓ | 4x base |
| 20px | 165 | ✓ | 5x base |
| 24px | 95 | ✓ | 6x base |
| 30px | 25 | ⚠️ | Should be 32px |
| 32px | 45 | ✓ | 8x base |
| 40px | 35 | ✓ | 10x base |
| 60px | 20 | ✓ | 15x base |

### Margin Usage Patterns

Similar to padding, with these additions:
- Negative margins used sparingly (-2px, -10px)
- Auto margins for centering
- Larger values for section spacing (80px, 100px)

### Common Spacing Patterns

#### Buttons
```css
/* Standard */
padding: 10px 20px; /* Should be 12px 20px for grid */
padding: 12px 24px; /* Perfect grid alignment */
padding: 16px 32px; /* Large buttons */
```

#### Cards
```css
padding: 20px;  /* Most common */
padding: 24px;  /* Also frequent */
margin-bottom: 24px; /* Card spacing */
```

#### Form Elements
```css
padding: 12px;      /* Inputs */
margin-bottom: 20px; /* Form groups */
gap: 8px;           /* Label to input */
```

### Border Radius Analysis

| Value | Usage | Use Case |
|-------|-------|----------|
| 4px | 25 | Small elements |
| 6px | 86 | Buttons, inputs (primary) |
| 8px | 43 | Cards, containers |
| 10px | 8 | Inconsistent |
| 12px | 23 | Large containers, modals |
| 20px | 16 | Pills, badges |
| 50% | 27 | Circular (avatars) |

## Recommendations

### Typography Scale (9 steps)
```css
--text-xs: 12px;    /* Small labels */
--text-sm: 14px;    /* Secondary text */
--text-base: 16px;  /* Body default */
--text-lg: 18px;    /* Emphasized body */
--text-xl: 20px;    /* Small headings */
--text-2xl: 24px;   /* Section headings */
--text-3xl: 28px;   /* Page headings */
--text-4xl: 32px;   /* Page titles */
--text-5xl: 48px;   /* Hero only */
/* Keep 64px, 80px, 120px for error pages only */
```

### Spacing Scale (4px base)
```css
--space-0: 0;
--space-1: 4px;
--space-2: 8px;
--space-3: 12px;
--space-4: 16px;
--space-5: 20px;
--space-6: 24px;
--space-8: 32px;
--space-10: 40px;
--space-12: 48px;
--space-15: 60px;
--space-20: 80px;
```

### Border Radius Scale
```css
--radius-sm: 4px;    /* Small elements */
--radius-base: 6px;  /* Default */
--radius-md: 8px;    /* Cards */
--radius-lg: 12px;   /* Modals */
--radius-pill: 20px; /* Pills */
--radius-full: 50%;  /* Circles */
```

## Files Needing Most Updates

1. **Spacing Issues**:
   - `vetting-application.html` - Uses 10px, 14px frequently
   - `event-checkin.html` - Mixed padding values
   - `admin-*` files - Inconsistent form spacing

2. **Typography Issues**:
   - `landing.html` - Uses 15px, 22px, 36px
   - `event-detail.html` - Mixed heading sizes
   - All files need font-weight standardization

## Implementation Notes

1. **Quick Wins**: 
   - Replace 10px → 8px or 12px
   - Replace 30px → 32px
   - Consolidate 0 vs 0px

2. **Careful Updates**:
   - 6px and 14px are used frequently but aren't on grid
   - Consider if these should remain as exceptions

3. **Preserve**:
   - Error page large typography (64px, 80px, 120px)
   - Negative margins where functionally required