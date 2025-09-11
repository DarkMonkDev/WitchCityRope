# Claude Code Session Handoff - Events Management System
**Date**: January 9, 2025
**Last Session End**: ~7:00 PM EST
**Branch**: `feature/2025-08-24-events-management`
**Worktree**: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/`

## 🎯 Session Starting Prompt for Next Claude Code

```
I'm continuing work on the WitchCityRope Events Management System. We just completed the Event Session Matrix Demo UI (tagged as v0.1.0-event-demo-ui-complete). 

Current status:
- Working in git worktree at: .worktrees/feature-2025-08-24-events-management/
- Demo UI complete at: http://localhost:5174/admin/event-session-matrix-demo
- TinyMCE rich text editor integrated with API key (hardcoded temporarily)
- All 4 tabs functional: Basic Info, Tickets/Orders, Emails, Volunteers
- Console errors fixed (reduced from 14 to 1)
- Backend requirements documented in: /docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/backend-integration-requirements.md

The UI demo is COMPLETE and we're ready to start the backend API implementation phase.

Please review:
1. The STATUS.md file in the events work folder for current milestone status
2. The backend-integration-requirements.md for next phase requirements
3. The Event Session Matrix architecture (Events → Sessions → TicketTypes → Tickets)

Next tasks are backend API development starting with the Events CRUD operations.
```

## 📍 Current Development State

### **Completed in This Session**
1. ✅ Event Session Matrix Demo UI - 100% complete
2. ✅ TinyMCE Integration - Working with API key: `3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp`
3. ✅ Wireframe Compliance - Exact match to approved designs
4. ✅ Console Error Fixes - 93% reduction (14 → 1)
5. ✅ Button Styling - Fixed text cutoff issues
6. ✅ Playwright Testing - Visual verification complete
7. ✅ Documentation - All progress tracked and requirements defined

### **Key Technical Details**

#### **TinyMCE Configuration**
- API Key: `3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp` (same for dev/prod)
- Currently hardcoded in `/apps/web/src/components/forms/TinyMCERichTextEditor.tsx`
- TODO: Fix environment variable loading (VITE_TINYMCE_API_KEY)
- Note: TinyMCE takes 3-5 seconds to initialize (normal CDN behavior)

#### **Running Services**
```bash
# API Service (Port 5653)
cd src/WitchCityRope.Api && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5653" dotnet run --no-launch-profile

# React Dev Server (Port 5174 in worktree)
cd /home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/apps/web && npm run dev
```

#### **Important URLs**
- Event Session Matrix Demo: http://localhost:5174/admin/event-session-matrix-demo
- TinyMCE Test Page: http://localhost:5174/test-tinymce
- API Health Check: http://localhost:5653/health

### **Git Status**
- Branch: `feature/2025-08-24-events-management`
- Last Commit: `605947d` - "docs: Add backend integration requirements for Events Management System"
- Tag: `v0.1.0-event-demo-ui-complete` - Marks UI completion milestone
- Working Directory: Clean

## 🚀 Next Phase: Backend API Implementation

### **Priority 1: Events API**
File: `/src/WitchCityRope.Api/Features/Events/`

```csharp
// Endpoints to implement:
GET    /api/events              // List all events
GET    /api/events/{id}         // Get event details
POST   /api/events              // Create event
PUT    /api/events/{id}         // Update event
DELETE /api/events/{id}         // Delete event
POST   /api/events/{id}/publish // Publish event
```

### **Priority 2: Event Sessions API**
```csharp
GET    /api/events/{eventId}/sessions
POST   /api/events/{eventId}/sessions
PUT    /api/events/{eventId}/sessions/{id}
DELETE /api/events/{eventId}/sessions/{id}
```

### **Priority 3: Ticket Types API**
```csharp
GET    /api/events/{eventId}/ticket-types
POST   /api/events/{eventId}/ticket-types
PUT    /api/events/{eventId}/ticket-types/{id}
DELETE /api/events/{eventId}/ticket-types/{id}
```

## 📋 Key Architecture Patterns

### **Event Session Matrix Model**
```
Event (Workshop/Performance/Social)
  └── Sessions (S1, S2, S3 - time slots)
       └── TicketTypes (defines which sessions included)
            └── Tickets (actual purchases by users)
```

### **Database Tables Needed**
- `Events` - Main event information
- `EventSessions` - Time slots for each event
- `EventTicketTypes` - Ticket configurations
- `EventTicketTypeSessions` - Junction table for ticket-session mapping
- `EventRegistrations` - User registrations/purchases
- `EventVolunteerPositions` - Volunteer opportunities

### **DTO Pattern**
- Use C# DTOs as source of truth
- NSwag auto-generates TypeScript interfaces
- NEVER create manual TypeScript interfaces for API data

## ⚠️ Critical Context

### **Lessons Learned This Session**
1. **Wireframes are PRIMARY source of truth** - No improvisation allowed
2. **Playwright testing is MANDATORY** - Must verify visually before claiming completion
3. **Button text cutoff fix**: Always use `minWidth` and `height` properties
4. **TinyMCE CDN**: 307 redirects are normal, 3-5 second init is expected
5. **Console errors**: Use Mantine components, not raw CSS classes

### **Known Issues**
1. **Environment Variable Loading**: VITE_TINYMCE_API_KEY not loading from .env.development
   - Temporary fix: API key hardcoded in TinyMCERichTextEditor.tsx
   - TODO: Debug why import.meta.env.VITE_TINYMCE_API_KEY returns undefined

2. **Mantine CSS Warning**: One expected CSS-in-JS warning remains (not critical)

### **Sub-Agent Usage**
- **backend-developer**: Use for API implementation
- **database-designer**: Use for schema design
- **test-developer**: Use for integration tests
- **git-manager**: Use for ALL git operations
- **librarian**: Use for documentation updates

## 📁 Important Files to Review

### **Requirements & Design**
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/backend-integration-requirements.md`
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/design/wireframes/event-form.html`
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/STATUS.md`

### **UI Implementation**
- `/apps/web/src/components/events/EventForm.tsx` - Main form component
- `/apps/web/src/pages/admin/EventSessionMatrixDemo.tsx` - Demo page
- `/apps/web/src/components/forms/TinyMCERichTextEditor.tsx` - Rich text editor

### **Lessons Learned**
- `/docs/lessons-learned/react-developer-lessons-learned.md` - Critical UI patterns
- `/.claude/agents/` - Sub-agent specific lessons

## 🎯 Success Criteria for Next Session

### **Backend Phase 1 Complete When:**
1. ✅ Events CRUD API implemented and tested
2. ✅ Database migrations created and applied
3. ✅ DTOs defined for NSwag generation
4. ✅ Integration tests passing
5. ✅ UI connected to real API (remove mock data)

### **Quality Gates:**
- All API endpoints return < 200ms
- 100% test coverage for business logic
- No breaking changes to UI
- Swagger documentation complete

## 💡 Pro Tips for Next Session

1. **Start with**: `cd .worktrees/feature-2025-08-24-events-management/`
2. **Check services**: Ensure API (5653) and React (5174) are running
3. **Review STATUS.md**: Get current milestone status
4. **Use sub-agents**: Delegate to appropriate agents, don't do everything yourself
5. **Test incrementally**: Implement one endpoint, test it, then move to next

## 📝 Session End Checklist
- [x] All changes committed
- [x] Documentation updated
- [x] Progress tracked in STATUS.md
- [x] Lessons learned documented
- [x] Tagged milestone: v0.1.0-event-demo-ui-complete
- [x] Handoff document created

---

**Ready for handoff to next Claude Code session. Backend API implementation is the next major milestone.**