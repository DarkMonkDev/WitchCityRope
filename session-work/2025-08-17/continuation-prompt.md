# Continuation Prompt for WitchCityRope React Migration
**Date**: August 17, 2025
**Project**: WitchCityRope - React Migration with Mantine v7

## COPY THIS ENTIRE PROMPT TO CONTINUE WORK:

---

I'm continuing work on the WitchCityRope React migration project. In our last session (August 17), we completed the Technology Research Phase and made critical infrastructure decisions. Please review the session handoff at `/home/chad/repos/witchcityrope-react/docs/standards-processes/session-handoffs/2025-08-17-infrastructure-testing-handoff.md` first.

## COMPLETED IN LAST SESSION:

1. **Created technology-researcher sub-agent** at `/.claude/agents/research/technology-researcher.md` for evaluating architecture decisions. This agent uses WebSearch, WebFetch, and context7 MCP for research.

2. **Selected Mantine v7 as UI Framework** (Score: 89/100 vs Chakra UI 81/100)
   - Created ADR-004 at `/docs/architecture/decisions/adr-004-ui-framework-mantine.md`
   - Updated all architecture docs to reflect Mantine v7
   - Key reasons: TypeScript-first, 120+ components, excellent forms, WCAG compliant

3. **Consolidated duplicate documentation**:
   - Merged 4 deployment docs → 1 at `/docs/guides-setup/docker-production-deployment.md`
   - Merged 3 CI/CD docs → 1 at `/docs/standards-processes/ci-cd/`
   - Archived all Blazor-specific content to `/docs/_archive/blazor-legacy/`

4. **Preserved business validation rules** from Blazor:
   - Extracted to `/docs/standards-processes/forms-validation-requirements.md`
   - Password: 8+ chars, upper/lower/digit/special
   - Scene Name: 2-50 chars, alphanumeric+underscore/hyphen, unique
   - Email: Valid format + async uniqueness check

5. **Created Mantine forms standardization**:
   - Components at `/apps/web/src/components/forms/`
   - Includes: EmailInput, PasswordInput, SceneNameInput with full validation
   - Uses Mantine use-form + Zod validation
   - 500ms debounce for async checks

6. **Updated agents for architecture awareness**:
   - UI Designer and React Developer now check `/docs/architecture/decisions/`
   - Prevents wrong framework usage
   - Single source of truth established

7. **Fixed orchestrate command** duplicate files:
   - Merged and deleted `/.claude/orchestrate-command.md`
   - Single source at `/.claude/commands/orchestrate.md`

8. **Installed Context7 MCP** for documentation access using the script at `/scripts/setup-context7-mcp.sh`

## CONFIRMED TECHNOLOGY STACK:
- **Frontend**: React + TypeScript + Vite + **Mantine v7** (NOT Chakra UI)
- **Backend**: .NET 9 + Entity Framework Core (NO CHANGES - not migrating from EF)
- **Database**: PostgreSQL (NO CHANGES)
- **Authentication**: httpOnly cookies via API endpoints
- **Forms**: Mantine use-form + Zod validation
- **Testing**: Playwright for E2E tests

## IMPORTANT CLARIFICATIONS:
- We are NOT moving away from Entity Framework - that was an error
- We ARE using Mantine v7, not Chakra UI (decision made via ADR-004)
- The migration is Blazor → React for FRONTEND ONLY
- Backend API stays with .NET 9 + Entity Framework

## WHAT TO DO NEXT:

### Priority 1: Create Form Components Test Page
Build a comprehensive test page to showcase all standardized form components:
```bash
cd /home/chad/repos/witchcityrope-react
npm install @mantine/core @mantine/hooks @mantine/form @mantine/dates @mantine/notifications
npm install -D @mantine/postcss-preset postcss postcss-preset-mantine
```

Create `/apps/web/src/pages/FormComponentsTest.tsx` that displays:
- **All form components** from `/apps/web/src/components/forms/`:
  - BaseInput, BaseSelect, BaseTextarea
  - EmailInput, PasswordInput, SceneNameInput, PhoneInput
  - EmergencyContactGroup
- **All interaction states**:
  - Default/empty state
  - Hover effects
  - Focus states
  - Filled states
  - Loading states (for async validation)
- **All error states**:
  - Required field errors
  - Format validation errors
  - Async validation errors (email/scene name uniqueness)
  - Success states after validation
- **Interactive features**:
  - Real-time validation feedback
  - Password strength meter updates
  - Phone number formatting
  - Debounced async checks

This test page will serve as:
- Visual confirmation of styling and interactions
- Documentation of component behavior
- Testing ground for branding/color updates later
- Reference for developers using the components

### Priority 2: Test Mantine v7 Infrastructure
After confirming form components look/work correctly, create a vertical slice:
- Member Profile page using the tested components
- Authentication check with httpOnly cookies
- API call to .NET backend
- Proper validation with business rules

### Priority 3: UI Branding/Design System
After Mantine works, create the WitchCityRope design system:
- Color palette for rope bondage community (professional but approachable)
- Typography standards
- Component customization
- Dark mode support
- Update the UI Designer agent's knowledge

### Priority 3: Authorization System Testing
Test role-based permissions (we've tested auth, not authorization):
- Admin role capabilities
- Teacher role capabilities  
- Vetted Member access
- General Member restrictions
- Guest limitations

### Priority 4: Begin Feature Migration
Start migrating features from Blazor using established patterns:
- Use orchestrator for complex features
- Follow forms standardization
- Maintain single source of truth

## CRITICAL REMINDERS:
1. **Check ADR-004** before any UI work - Mantine v7 is the chosen framework
2. **Use technology-researcher** agent (via orchestrator) for any new tech evaluations
3. **Forms must follow** `/docs/standards-processes/forms-validation-requirements.md`
4. **No duplicate documentation** - check file registry before creating new docs
5. **Context7 is installed** - use "use context7" in prompts for latest docs

## FILES TO REVIEW FIRST:
1. `/home/chad/repos/witchcityrope-react/docs/standards-processes/session-handoffs/2025-08-17-infrastructure-testing-handoff.md`
2. `/home/chad/repos/witchcityrope-react/PROGRESS.md`
3. `/home/chad/repos/witchcityrope-react/docs/architecture/decisions/adr-004-ui-framework-mantine.md`
4. `/home/chad/repos/witchcityrope-react/docs/standards-processes/forms-validation-requirements.md`

## WORKING DIRECTORIES:
- Migration docs: `/home/chad/repos/witchcityrope-react/docs/architecture/react-migration/`
- Test projects: `/home/chad/repos/witchcityrope-react/session-work/2025-08-17/`
- Forms components: `/home/chad/repos/witchcityrope-react/apps/web/src/components/forms/`

Please start by reviewing the handoff document and PROGRESS.md, then we can begin testing the Mantine v7 infrastructure with a simple vertical slice feature.

---

END OF CONTINUATION PROMPT