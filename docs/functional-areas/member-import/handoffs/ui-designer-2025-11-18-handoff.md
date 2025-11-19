# UI Designer Handoff - Email Admin UI
**Date**: 2025-11-18
**Phase**: Phase 3A - UI Design
**Feature**: Email Admin Enhancement - Send Ad-Hoc Email UI
**Status**: ✅ COMPLETE

## 🎯 COMPLETED TASKS

1. ✅ **Send Ad-Hoc Email Section**: Designed for Ad Hoc panel
2. ✅ **Segment Selector**: Dropdown with recipient count
3. ✅ **Preview Recipients**: Show first 10 users from segment
4. ✅ **Send Confirmation**: Dialog before sending
5. ✅ **Success/Error States**: Visual feedback
6. ✅ **Wireframes**: Desktop and mobile layouts
7. ✅ **Component Breakdown**: Complete specifications
8. ✅ **API Integration**: Documented all endpoints
9. ✅ **User Flows**: Happy path and error scenarios
10. ✅ **Responsive Design**: All breakpoints specified

## 📍 DESIGN DOCUMENT

**Location**: `/home/chad/repos/witchcityrope/docs/functional-areas/member-import/email-send-ui-design.md`

This comprehensive design document includes:
- Complete wireframes (desktop + mobile)
- 6 component specifications with props interfaces
- State management architecture
- API integration points (3 endpoints)
- User flow diagrams (4 scenarios)
- Responsive behavior specifications
- Accessibility requirements (WCAG 2.1 AA)
- Design tokens and Mantine v7 component usage
- Error handling for all scenarios
- Integration with existing EmailCategoryPanel

## 🔑 KEY DESIGN DECISIONS

### 1. Integration Approach
- **Decision**: Add to existing EmailCategoryPanel, only visible on Ad Hoc tab
- **Rationale**: Maintains consistency, reuses existing patterns, minimal disruption
- **Pattern**: Divider separator + new section below template editor

### 2. Component Structure
```
SendAdHocEmailSection (main)
  ├── RecipientSelector (dropdown + count display)
  ├── EmailContentEditor (subject + rich text + variables)
  ├── RecipientPreview (first 10 recipients)
  ├── SendActions (cancel + send buttons)
  └── SendConfirmationModal (safety confirmation)
```

### 3. User Flow
1. Select segment → Auto-load preview
2. Compose email → Real-time validation
3. Click send → Confirmation modal
4. Confirm → API call → Success notification → Form reset

### 4. Safety Features
- **Preview Recipients**: Shows first 10 before send
- **Confirmation Modal**: Requires explicit "Send Now" click
- **Variable Validation**: Real-time invalid variable detection
- **Form Preservation**: Content saved if send fails (can retry)

### 5. Responsive Strategy
- **Desktop**: Horizontal buttons, full rich text toolbar
- **Mobile**: Stacked buttons (Send on top), simplified toolbar
- **Touch Targets**: 48×48px minimum on mobile

## 📊 USER SEGMENT OPTIONS

The dropdown will show 8 segments with live counts:

| Segment | Definition | Example Count |
|---------|-----------|---------------|
| All Vetted Members | VettingStatus == Approved | 142 |
| All Pre-Vetted Members | Not Denied/OnHold, IsActive | 158 |
| All Teachers | Role contains "Teacher" | 15 |
| All DMs | Role contains "DungeonMonitor" | 8 |
| All Safety Team | Role contains "SafetyTeam" | 6 |
| All Admins | Role contains "Administrator" | 5 |
| Email Not Verified | EmailVerified == false | 142 |
| Vetting Pending | VettingStatus == UnderReview | 23 |

## 🎨 DESIGN SYSTEM COMPLIANCE

### Colors (Design System v7)
- **Primary**: Burgundy #880124
- **Text**: Charcoal #2B2B2B
- **Accents**: Rose gold #B76D75
- **Success**: Green #228B22
- **Warning**: Brass #DAA520
- **Error**: Crimson #DC143C

### Typography
- **Headings**: Montserrat, 600-800 weight, uppercase
- **Body**: Source Sans 3, 400-600 weight
- **Section Title**: 32px (desktop), 24px (mobile)
- **Card Text**: 14px

### Components (Mantine v7)
- Stack, Group, Box, Paper (layout)
- Select (segment dropdown)
- TextInput (subject)
- MantineTiptapEditor (rich text)
- Button (actions)
- Modal (confirmation)
- Alert (validation warnings)

## 🔌 API ENDPOINTS NEEDED

Backend developer will implement these:

1. **GET /api/email-templates/segments**
   - Returns: `UserSegmentDto[]` with name, displayName, count
   - Used by: RecipientSelector to populate dropdown

2. **GET /api/email-templates/segments/{name}/preview**
   - Returns: `PreviewRecipientDto[]` (first 10)
   - Used by: RecipientPreview to show recipient list

3. **POST /api/email-templates/ad-hoc/send**
   - Request: `{ segment, subject, htmlBody }`
   - Response: `{ success, sentCount, failedCount, errors }`
   - Used by: Send mutation to send emails

## ✅ VALIDATION CHECKLIST

- [x] Reviewed existing Email Templates Admin UI
- [x] Designed consistent with Mantine v7 patterns
- [x] Matches existing color scheme
- [x] All user flows documented
- [x] Error states designed
- [x] Accessibility considered
- [x] Created comprehensive design document
- [x] Wireframes complete (desktop + mobile)
- [x] Component specifications complete
- [x] State management documented
- [x] API integration points defined
- [x] Responsive behavior specified

## 🚀 NEXT AGENT: react-developer (Phase 3B)

**Handoff Package for React Developer**:

1. **Design Document**: `/docs/functional-areas/member-import/email-send-ui-design.md`
2. **Existing Code to Study**:
   - `/apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx`
   - `/apps/web/src/components/email-templates/EmailCategoryPanel.tsx`
   - `/apps/web/src/components/forms/MantineTiptapEditor.tsx`
3. **Files to Create**:
   - `/apps/web/src/components/email-templates/SendAdHocEmailSection.tsx`
4. **Files to Modify**:
   - `/apps/web/src/components/email-templates/EmailCategoryPanel.tsx` (add conditional rendering)
   - `/apps/web/src/services/emailTemplates.api.ts` (add API methods)
5. **Backend Dependencies**:
   - Wait for Phase 2A (backend-developer) to complete API endpoints
   - Can implement UI with mock data initially

**Critical Implementation Notes**:

- **Variable Validation**: Copy pattern from existing EmailCategoryPanel (lines 96-123)
- **Rich Text Editor**: Use existing MantineTiptapEditor component (already imports correctly)
- **Button Styling**: Follow Button Style Guide exactly (no custom overrides)
- **Mantine Components**: Use Mantine v7 components, not custom styled divs
- **API Integration**: Use React Query (useQuery + useMutation) pattern from EmailCategoryPanel
- **Notifications**: Use Mantine notifications system (already imported in EmailCategoryPanel)

**Testing Priorities**:

1. Segment selection → Preview load → Send flow
2. Variable validation (real-time)
3. Confirmation modal → Send → Success notification → Form reset
4. Error handling → Form preservation → Retry
5. Responsive behavior (desktop + mobile)
6. Accessibility (keyboard navigation, screen reader)

---

**Design Status**: ✅ COMPLETE
**Design Document**: `/docs/functional-areas/member-import/email-send-ui-design.md`
**Handoff Created**: 2025-11-18
**Next Phase**: Phase 3B - React Implementation
