# Per-Ticket-Purchase Cancellation Flags

**Date**: 2025-12-11
**Status**: Ready for Implementation
**Type**: Bug Fix / Enhancement

## Overview

This feature adds per-ticket-purchase cancellation eligibility tracking to fix a bug where multi-session event ticket cancellation is incorrectly blocked based on the event's start date rather than individual session timing.

## Problem

In multi-session events, users cannot cancel tickets for future sessions if they have tickets for sessions that are starting soon, because the system checks the event's overall start date instead of checking each ticket purchase's sessions individually.

## Solution

Add `CanCancel` and `CancellationMessage` properties to `TicketPurchaseInfoDto` so that each ticket purchase's cancellation eligibility is calculated based on its own reference session timing, not the event's start date.

## Key Changes

### Backend
- **DTO Update**: Add `CanCancel` and `CancellationMessage` to `TicketPurchaseInfoDto`
- **Service Logic**: Calculate per-purchase cancellation eligibility in `AttendanceService.GetParticipationStatusAsync`

### Frontend
- **UI Enhancement**: Show disabled checkboxes with explanation messages for non-cancelable tickets
- **User Experience**: Users can cancel future tickets even if they have imminent tickets

## Documents

- **Implementation Plan**: [implementation-plan.md](./implementation-plan.md) - Complete step-by-step implementation guide

## Timeline

- **Created**: 2025-12-11
- **Estimated Implementation**: 1-2 days
- **Complexity**: Medium (backend service logic changes + frontend UI updates)

## Related Work

- **Parent Feature**: RSVP/Ticketing System (`/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/`)
- **Related Feature**: Session-Based Ticketing (`/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/`)
