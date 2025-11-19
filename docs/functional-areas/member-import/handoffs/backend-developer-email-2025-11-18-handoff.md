# Backend Developer Handoff - Email Segmentation
**Date**: 2025-11-18
**Phase**: Phase 2A - Email Segmentation Backend
**Feature**: Email Admin Enhancement - Segment-Based Sending

## 🎯 CRITICAL TASKS

1. **UserSegment Enum**: Create with 8 segment types
2. **GET /api/email-templates/segments**: Return all segments with user counts
3. **GET /api/email-templates/segments/{name}/preview**: Return first 10 users
4. **Enhance SendAdHocEmailAsync**: Support segment-based sending
5. **NewWebsiteUser Template**: Add to EmailTemplateSeeder
6. **Variable Replacement**: Support {{user_name}}, {{reset_url}}
7. **Integration Tests**: Comprehensive test coverage

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Orchestrator Handoff | `/docs/functional-areas/member-import/handoffs/orchestrator-2025-11-18-handoff.md` | Email Enhancement Requirements, User Segment Definitions |

## 🚨 KNOWN REQUIREMENTS

### User Segment Definitions
- **AllVettedMembers**: VettingStatus == 3 (Approved)
- **AllPreVettedMembers**: VettingStatus NOT IN (4=Denied, 5=OnHold) AND IsActive=true
- **AllTeachers**: Role contains "Teacher"
- **AllDMs**: Role contains "DungeonMonitor"
- **AllSafetyTeam**: Role contains "SafetyTeam"
- **AllAdmins**: Role contains "Administrator"
- **EmailNotVerified**: EmailVerified == false
- **VettingPending**: VettingStatus == 0 (UnderReview)

## ✅ VALIDATION CHECKLIST

- [ ] UserSegment enum created with 8 types
- [ ] GET /api/email-templates/segments returns counts
- [ ] GET /api/email-templates/segments/{name}/preview returns users
- [ ] SendAdHocEmailAsync supports segments
- [ ] NewWebsiteUser template added to seeder
- [ ] Variable replacement working
- [ ] Integration tests pass
- [ ] API documentation updated

## 📝 DELIVERABLES

1. UserSegment enum
2. Segments API endpoint
3. Preview API endpoint
4. Enhanced SendAdHocEmailAsync
5. NewWebsiteUser email template
6. Variable replacement logic
7. Integration tests
8. API documentation

---

**Status**: PLACEHOLDER - Awaiting orchestrator delegation
**Depends On**: Phase 1B complete
**Next Agent**: react-developer (email UI)
