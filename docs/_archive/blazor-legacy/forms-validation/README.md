# Blazor Forms and Validation Legacy Documentation

**Archive Date:** 2025-08-17  
**Reason:** Migrating from Blazor Server to React + TypeScript  
**Replacement:** See `/docs/standards-processes/forms-validation-requirements.md`

## Contents

This archive contains all Blazor-specific form and validation documentation that was created for the Blazor Server implementation. While the specific component implementations are no longer applicable for React, the business requirements, validation rules, and accessibility standards contained within are still relevant.

## Archived Files

1. **form-fields-and-validation-standards.md** - Blazor validation component usage patterns
2. **FORM_MIGRATION_STATUS_FINAL.md** - Blazor component migration completion status
3. **FORM_MIGRATION_VERIFICATION_COMPLETE.md** - Blazor migration verification results
4. **validation-standardization/** - Complete Blazor validation system
   - VALIDATION_COMPONENT_LIBRARY.md - WCR Blazor components
   - VALIDATION_STANDARDS.md - Blazor validation architecture
   - best-practices.md - Blazor Server validation patterns
   - current-state.md - jQuery to Blazor migration analysis
5. **forms-standardization.md** - React forms approach (keep for React reference)

## What to Extract for React

### Business Requirements (Still Applicable)
- Password complexity requirements
- Email validation patterns
- Scene name validation rules
- Form field naming conventions
- Error message text standards
- Accessibility (ARIA) requirements
- Required field indicators

### Technical Patterns (Archive Only)
- Blazor EditForm components
- DataAnnotationsValidator usage
- WCR Blazor validation components
- Blazor-specific CSS classes
- ASP.NET Core validation attributes

## Migration Notes

The React implementation should preserve:
1. All business validation rules
2. Error message text and tone
3. Accessibility standards
4. Field labeling conventions
5. Required field visual indicators
6. Form structure patterns

See the consolidated requirements document for React-specific implementation guidance.
