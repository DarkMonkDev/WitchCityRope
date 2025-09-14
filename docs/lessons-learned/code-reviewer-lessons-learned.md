# Code Reviewer Lessons Learned

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## Prevention Patterns

### File Path Verification Before Review
**Problem**: Code reviews reference incorrect file paths causing review failures and implementation delays.
**Solution**: Always verify exact file paths exist before starting reviews - use absolute paths and check filesystem.

### Standards Document Existence Check
**Problem**: Reviews reference non-existent standards documents causing inconsistent implementation guidance.
**Solution**: Verify referenced standards documents exist at documented paths before including in review comments.

### Backend Pattern Cross-Reference
**Problem**: Frontend reviews miss backend integration issues causing API mismatches and runtime failures.
**Solution**: Check backend patterns in `/apps/api/` when reviewing frontend API integration code.

### UI Component Pattern Validation
**Problem**: Component reviews miss design system violations causing inconsistent user interface.
**Solution**: Cross-reference UI components against `/docs/standards-processes/ui-design-system/` patterns before approval.

### Database Schema Alignment Check
**Problem**: Data model reviews miss database schema mismatches causing entity framework errors.
**Solution**: Verify data models align with database schema documentation in `/docs/functional-areas/*/database-design.md`.

### Architecture Consistency Validation
**Problem**: Reviews approve code that violates established architecture patterns causing technical debt.
**Solution**: Check implementation follows vertical slice architecture documented in technical design specifications.

## Mistakes to Avoid

- **Missing dependency verification**: Check that imports and dependencies actually exist in the codebase
- **Incomplete security review**: Always verify authentication/authorization patterns match documented standards  
- **Performance pattern ignorance**: Review for N+1 queries, unoptimized loops, and blocking async calls
- **Test coverage gaps**: Ensure reviewed code includes corresponding test implementations
- **Documentation inconsistency**: Verify code changes align with existing functional documentation
- **API contract violations**: Check that endpoint changes maintain backward compatibility
- **Configuration oversight**: Review environment-specific configuration changes for all deployment targets