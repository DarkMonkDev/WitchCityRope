# Implementation Plan: Dashboard Extraction & Payment Processor Migration
**Date**: 2025-09-13
**Owner**: Orchestrator
**Status**: Ready for Execution

## Executive Summary

This plan addresses two critical requirements:
1. **Dashboard Features Extraction** - Extract only the user-focused dashboard features (not admin analytics)
2. **Payment Processor Migration** - Replace Stripe with PayPal and Venmo integration

## Part 1: Dashboard Features Extraction

### Features to Extract (User Confirmed)
Based on investigation and user feedback:

#### ✅ **EXTRACT**
- **Event attendance tracking** - User's personal event history
- **Basic user dashboard** - Profile, vetting status, membership info
- **Administrative event management** - Already implemented in modern API

#### ❌ **DO NOT EXTRACT**  
- **Financial summaries** - Use external tools (QuickBooks, PayPal reports)
- **User activity statistics** - Consider separate reporting system later
- **Community engagement metrics** - Not needed

### Dashboard Implementation Plan (1-2 days)

#### Phase 1: Backend (Day 1 Morning)
**Agent**: backend-developer
1. Extract 3 dashboard endpoints from legacy API
2. Create `/apps/api/Features/Dashboard/`
3. Implement services:
   - `UserDashboardService` - Personal dashboard data
   - `EventAttendanceService` - User's event history
4. Create endpoints:
   - `GET /api/dashboard/user/{userId}`
   - `GET /api/dashboard/user/{userId}/events`
   - `GET /api/dashboard/user/{userId}/statistics`

#### Phase 2: Frontend (Day 1 Afternoon)
**Agent**: react-developer
1. Create `/apps/web/src/features/dashboard/`
2. Build components:
   - `UserDashboard.tsx` - Main dashboard view
   - `EventHistory.tsx` - Past/upcoming events
   - `MembershipStats.tsx` - Tenure, attendance
3. Add route `/dashboard` to router

#### Phase 3: Testing (Day 2 Morning)
**Agent**: test-executor
1. Test dashboard data aggregation
2. Verify user permissions
3. E2E dashboard flow

---

## Part 2: Payment Processor Migration (Stripe → PayPal/Venmo)

### Critical Business Context
- **Current**: Implementation uses Stripe (incorrect)
- **Required**: PayPal and Venmo are the actual payment processors
- **Impact**: All payment code needs refactoring

### Migration Strategy

#### Option A: Complete Replacement (Recommended)
**Timeline**: 3-4 days
1. Remove all Stripe code
2. Implement PayPal SDK integration
3. Add Venmo support through PayPal
4. Maintain sliding scale pricing

#### Option B: Adapter Pattern
**Timeline**: 2-3 days
1. Create payment processor abstraction
2. Implement PayPal adapter
3. Implement Venmo adapter
4. Switch configuration

### PayPal/Venmo Implementation Plan

#### Phase 1: Requirements & Design (Day 1)
**Agent**: business-requirements
1. Document PayPal business account requirements
2. Define Venmo integration approach (via PayPal or separate)
3. Update payment flow diagrams
4. Sliding scale compatibility verification

#### Phase 2: Backend Refactoring (Day 2-3)
**Agent**: backend-developer

**Remove Stripe Dependencies:**
```csharp
// Remove from PaymentService.cs
- IStripeService
- Stripe.net package
- StripePaymentIntentId fields

// Add PayPal Dependencies:
+ PayPalCheckoutSdk package
+ IPayPalService
+ PayPalOrderId fields
```

**New Services:**
1. `PayPalService.cs` - PayPal Checkout SDK integration
2. `VenmoService.cs` - Venmo through PayPal or standalone
3. Update `PaymentService.cs` to use new processors

**Database Changes:**
- Rename `StripePaymentIntentId` → `PayPalOrderId`
- Add `VenmoTransactionId` field
- Update payment method enum

#### Phase 3: Frontend Refactoring (Day 3-4)
**Agent**: react-developer

**Remove Stripe Components:**
```typescript
// Remove from payment components
- @stripe/stripe-js
- @stripe/react-stripe-js
- CardElement components

// Add PayPal Components:
+ @paypal/react-paypal-js
+ PayPalButtons component
+ Venmo button option
```

**Update Payment Flow:**
1. Replace Stripe Elements with PayPal buttons
2. Add Venmo as payment option
3. Maintain sliding scale interface (unchanged)
4. Update confirmation flow

#### Phase 4: Testing & Validation (Day 4)
**Agent**: test-executor
1. Test PayPal sandbox transactions
2. Test Venmo integration
3. Verify sliding scale calculations
4. E2E payment flows

---

## Workflow & Process Requirements

### Documentation
- Create handoff documents between phases
- Update lessons learned after each phase
- Document API changes

### Commits
- Commit after each service implementation
- Commit after each component completion
- Use descriptive commit messages

### Sub-Agent Usage
1. **backend-developer** - Backend implementation
2. **react-developer** - Frontend components
3. **test-executor** - Testing each phase
4. **business-requirements** - PayPal/Venmo requirements

### Handoff Points
1. After Dashboard backend → Frontend team
2. After PayPal requirements → Backend team
3. After backend refactor → Frontend team
4. After implementation → Test team

---

## Timeline Summary

### Week 1 (Next 5 days)
- **Day 1-2**: Dashboard extraction (complete)
- **Day 3-5**: PayPal/Venmo migration

### Deliverables
1. User dashboard features (3 endpoints)
2. PayPal payment processing
3. Venmo payment support
4. Updated documentation
5. Test coverage

---

## Risk Mitigation

### Dashboard Risks
- **Low risk** - Simple data aggregation
- **Mitigation**: Use existing patterns

### Payment Migration Risks
- **High risk** - Core business function
- **Mitigation**: 
  - Extensive testing in sandbox
  - Feature flag for rollback
  - Maintain payment audit logs

---

## Questions for User

1. **PayPal Account**: Do you have a PayPal Business account set up?
2. **Venmo Integration**: Should Venmo be through PayPal or separate?
3. **Transition Period**: Should we support both Stripe and PayPal temporarily?
4. **Testing**: Do you have PayPal sandbox credentials?

---

## Next Steps

1. ✅ User approves this plan
2. 🚀 Begin Dashboard extraction (Day 1)
3. 📋 Gather PayPal/Venmo requirements
4. 🔧 Execute payment migration
5. 🧪 Comprehensive testing

This plan follows all workflow processes, uses proper sub-agents, includes documentation/handoffs, and ensures frequent commits throughout the implementation.