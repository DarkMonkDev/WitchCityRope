# Form Implementation Lessons Learned

## Critical Prevention Patterns

### Always Use Framework Components - 2025-08-18
**Problem**: Major time waste from creating custom HTML instead of using Mantine components.
**Solution**: Always start with framework components (TextInput, PasswordInput), use styles prop for customization.
**Prevention**: Check framework component library first, never wrap custom HTML in framework styling.

### Position Labels Relative to Input Containers - 2025-08-18
**Problem**: Floating labels positioned inconsistently when helper text is present.
**Solution**: Create isolated input containers excluding helper text, position labels relative to these.
**Prevention**: Separate label positioning containers from helper text containers.

### Hide Placeholders by Default with Floating Labels - 2025-08-18
**Problem**: Visual conflicts between placeholders and floating labels.
**Solution**: Hide placeholders by default, show only on focus when empty using CSS-only approach.
**Prevention**: Design placeholder visibility to complement floating labels from start.

### Password Inputs Need Special CSS Targeting - 2025-08-18
**Problem**: Password inputs have different structure, require additional selectors.
**Solution**: Target password-specific classes and data attributes beyond standard input selectors.
**Prevention**: Test all input types individually, don't assume text input patterns work everywhere.

### Focus States Need Both Outline Removal AND Border Changes - 2025-08-18
**Problem**: Users expect border color changes, not just outline removal.
**Solution**: Remove framework focus outlines AND add brand-appropriate border color changes.
**Prevention**: Implement both outline removal and border color changes as separate concerns.

### Be Precise in Requirements to Prevent Circular Debugging - 2025-08-18
**Problem**: Vague requirements lead to implementations that miss actual needs.
**Solution**: Specify exact visual behavior, include all input types, describe edge cases.
**Prevention**: Always provide visual examples and success criteria in requirements.

## Common Mistakes to Avoid

- **Custom HTML instead of framework components**: Always start with Mantine component library
- **Label positioning including helper text**: Create isolated input containers for consistent positioning
- **Assuming text input CSS works for passwords**: Test password inputs separately with additional selectors
- **Placeholder/floating label conflicts**: Hide placeholders by default, show on focus when empty
- **Missing focus visual feedback**: Target actual input elements with brand colors, not wrapper divs
- **Vague agent requirements**: Specify behavior for all input types and edge cases

## Essential CSS Patterns

```css
/* Framework components only */
<TextInput styles={{ input: { borderColor: 'var(--brand-color)' } }} />

/* Isolated positioning containers */
<Box className="inputContainer">
  <TextInput />
  <Text className="floatingLabel" />
</Box>

/* Hidden placeholders by default */
.input::placeholder { opacity: 0; }
.input:focus::placeholder { opacity: 1; }

/* Complete focus states */
.input:focus {
  outline: none !important;
  border-color: var(--brand-color) !important;
}
```