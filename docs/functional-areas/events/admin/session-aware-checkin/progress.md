# Session-Aware Check-In Feature

**Started**: 2025-11-30
**Status**: Phase 1 - Database Design
**Work Type**: Feature Enhancement

## Summary
Add session-level granularity to the kiosk check-in system for multi-session events.

## Key Changes
- Add SessionId to CheckIn entity
- Add SessionId to CheckInSessionToken entity
- Update token generation modal with session selector
- Update check-in validation to respect ticket session scope
- Show sessions attended in Attendees tab

## Progress
- [x] Analysis complete
- [ ] Database design
- [ ] Backend implementation
- [ ] Frontend implementation
- [ ] Testing
