# Documentation Quick Reference
<!-- Last Updated: 2025-08-04 -->

## Document Header Template
```markdown
# Title
<!-- Last Updated: YYYY-MM-DD -->
<!-- Version: X.Y -->
<!-- Owner: Team Name -->
<!-- Status: Active|Draft|Deprecated -->
```

## Common Locations
- **Project Status**: `/PROGRESS.md`
- **Architecture**: `/docs/architecture/`
- **Feature Docs**: `/docs/functional-areas/[feature]/`
- **Standards**: `/docs/standards-processes/`
- **Navigation**: `/docs/00-START-HERE.md`

## File Naming
- Features: `feature-name.md` (lowercase, hyphens)
- Guides: `GUIDE_NAME.md` (uppercase, underscores)
- Status: `status-YYYY-MM-DD.md`

## Creating New Feature Docs
```bash
cp -r docs/functional-areas/_template docs/functional-areas/new-feature
```

Then update:
1. `README.md` - Overview
2. `current-state/business-requirements.md`
3. `current-state/functional-design.md`
4. `current-state/user-flows.md`
5. `current-state/test-coverage.md`

## Quick Checklist
- [ ] Added header with metadata
- [ ] Included overview section
- [ ] Used clear headings
- [ ] Added code examples
- [ ] Linked related docs
- [ ] Spell checked
- [ ] Updated navigation

## Archiving Old Docs
```bash
mkdir -p docs/_archive/[area]/
mv old-doc.md docs/_archive/[area]/
# Create ARCHIVE_README.md explaining why
```

## Need Help?
See full guide: `/docs/standards-processes/documentation-process/DOCUMENTATION_GUIDE.md`