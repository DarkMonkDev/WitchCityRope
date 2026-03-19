# Implementation Plan: Buy Tickets / RSVP for Others from Event Detail Page

<!-- Created: 2026-03-19 -->
<!-- Status: Approved - Implementation in progress -->

## Design Notes from Stakeholder
- When user already has a ticket and enters checkout in "buy for others" mode, the assignment dropdown appears IMMEDIATELY on ticket selection
- No "Your ticket (YOU)" row, no "Assign later" default
- Every ticket row is an assignment row from the start

## Summary
See main conversation for full plan details. This document serves as a reference marker.
Key flag: `BuyForOthersOnly` on CreateTicketPurchaseRequest/CheckoutRequest.
6 backend changes, 4 frontend changes, 6 implementation phases.
