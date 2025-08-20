# MCP Tools for Style Guide Implementation

## Overview
Model Context Protocol (MCP) servers are tools that extend Claude's capabilities. These free tools can help automate and streamline the style guide creation and application process.

## Recommended MCP Tools for This Project

### 1. Design System MCP (Primary Tool)
**Purpose**: Manages design tokens and component properties
**Repository**: `yajihum/design-system-mcp`

**Installation**:
```bash
npm install -g @modelcontextprotocol/create-server
npx @modelcontextprotocol/create-server install design-system-mcp
```

**Usage for Witch City Rope**:
- Store color tokens, spacing scales, typography
- Manage component properties
- Generate consistent CSS variables
- Maintain design system documentation

### 2. MCP Design System Extractor
**Purpose**: Extracts and analyzes existing styles from HTML
**Use Case**: Analyze current wireframes to extract patterns

**How to use**:
1. Point it at the wireframes directory
2. Extract all CSS styles
3. Identify common patterns
4. Generate report of inconsistencies

### 3. Shadcn-vue MCP (If Moving to Tailwind)
**Purpose**: Component generation with built-in styling
**Repository**: `HelloGGX/shadcn-vue-mcp`

**Benefits**:
- Generates accessible components
- Consistent Tailwind classes
- TypeScript support
- Built-in dark mode

### Alternative Approach: CSS-in-Blazor
Since you're using Blazor with Syncfusion, you might prefer:
- Using Syncfusion's built-in theming
- Creating Blazor components with isolated CSS
- Implementing CSS variables for design tokens

## Implementation Strategy

### Step 1: Extract Current Styles
```javascript
// Pseudo-code for extraction
const files = glob('./wireframes/*.html');
const styles = extractStyles(files);
const report = analyzeInconsistencies(styles);
```

### Step 2: Create Design Tokens
```json
{
  "colors": {
    "primary": "#8B4513",
    "primary-hover": "#6B3410",
    "success": "#2e7d32",
    "warning": "#f57c00",
    "error": "#d32f2f"
  },
  "spacing": {
    "xs": "4px",
    "sm": "8px",
    "md": "16px",
    "lg": "24px",
    "xl": "32px"
  },
  "typography": {
    "h1": {
      "size": "32px",
      "weight": "700",
      "line": "1.2"
    }
  }
}
```

### Step 3: Generate Component Library
Using MCP tools to create consistent components:
- Button component with all variants
- Form controls with validation states
- Card layouts
- Navigation patterns

### Step 4: Apply to Wireframes
Systematic replacement of inline styles with:
- CSS variables from design tokens
- Component classes
- Utility classes for spacing

## Manual Alternative (Without MCP)

If MCP tools aren't working well with your setup:

### 1. Create Master CSS File
```css
/* design-tokens.css */
:root {
  /* Colors */
  --color-primary: #8B4513;
  --color-primary-hover: #6B3410;
  
  /* Spacing */
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  
  /* Typography */
  --font-size-h1: 32px;
  --font-weight-bold: 700;
}
```

### 2. Component CSS
```css
/* components.css */
.btn {
  padding: var(--space-3) var(--space-5);
  border-radius: 6px;
  font-weight: 500;
  transition: all 0.2s;
}

.btn-primary {
  background: var(--color-primary);
  color: white;
}

.btn-primary:hover {
  background: var(--color-primary-hover);
}
```

### 3. Update Wireframes
Replace inline styles with classes:
```html
<!-- Before -->
<button style="background: #8B4513; padding: 12px 24px;">

<!-- After -->
<button class="btn btn-primary">
```

## Integration with Blazor/Syncfusion

### Option 1: Syncfusion Theme Customization
```css
/* syncfusion-theme-override.css */
.e-btn.e-primary {
  background-color: var(--color-primary);
}
```

### Option 2: Blazor Component Library
```razor
<!-- Button.razor -->
<button class="btn btn-@Type btn-@Size" @onclick="OnClick">
    @ChildContent
</button>

@code {
    [Parameter] public string Type { get; set; } = "primary";
    [Parameter] public string Size { get; set; } = "regular";
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
}
```

## Validation Tools

### Accessibility
- Wave browser extension
- axe DevTools
- Lighthouse audits

### CSS Consistency
- Stylelint configuration
- CSS validation service
- Custom rules for design tokens

## Maintenance

### Design Token Updates
1. Update token file
2. Regenerate CSS variables
3. Test affected components
4. Deploy changes

### Component Updates
1. Update component definition
2. Test in isolation
3. Update documentation
4. Apply across wireframes

## Resources
- [MCP Documentation](https://github.com/anthropics/model-context-protocol)
- [Syncfusion Blazor Themes](https://blazor.syncfusion.com/documentation/appearance/themes)
- [CSS Custom Properties Guide](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)
- [Design Tokens W3C Spec](https://design-tokens.github.io/community-group/format/)