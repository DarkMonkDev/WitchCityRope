# Business Requirements Handoff - Payment System Extraction
<!-- Date: 2025-09-13 -->
<!-- From: Business Requirements Agent -->
<!-- To: UI Designers, Functional Spec Agents, Backend Developers, Test Developers -->
<!-- Status: Complete -->

## Handoff Summary

Complete business requirements analysis for Payment system extraction from legacy API to modern API. This represents a critical component of the API modernization effort with **ZERO existing payment functionality** in the modern API, requiring complete greenfield implementation.

## Critical Business Rules (MUST IMPLEMENT)

### 1. Sliding Scale Pricing (NON-NEGOTIABLE)
- **Honor System**: 0-75% discount available to ALL community members
- **No Verification Required**: Absolutely no documentation or proof of need
- **Community Value**: Core principle of economic inclusivity
- **Privacy**: Sliding scale usage must remain confidential
- **Implementation**: Automatic calculation based on user selection

### 2. Payment Security Requirements
- **PCI Compliance**: All payment processing through Stripe, zero card storage
- **Encryption**: Sensitive payment metadata encrypted at rest
- **Audit Trail**: Complete transaction logging for all payments
- **Access Control**: Payment data restricted to admin/organizer roles only
- **Fraud Prevention**: Rate limiting and transaction validation

### 3. Registration Integration
- **Status Updates**: Payment success MUST automatically confirm registration
- **Capacity Management**: Payment processing holds event capacity
- **Email Notifications**: Payment confirmation triggers registration email
- **Failed Payment Handling**: Failed payments move to waitlist until resolved

## User Stories with Acceptance Criteria

### MVP Stories (Phase 1 - 2 weeks)

#### Story 1: Event Registration Payment
- **Core Flow**: Member → Event → Register → Sliding Scale Selection → Payment → Confirmation
- **Critical**: Sliding scale 0-75% discount calculation
- **Security**: Stripe Elements integration for PCI compliance
- **Integration**: Automatic registration confirmation on payment success

#### Story 2: Payment Method Management
- **Requirement**: Save payment methods for returning users
- **Security**: Stripe customer tokens, no local card storage
- **UX**: Easy payment method selection for future transactions

#### Story 3: Admin Refund Processing
- **Business Rule**: Only completed payments can be refunded
- **Validation**: Refund amount cannot exceed original payment
- **Workflow**: Admin → Select Payment → Initiate Refund → Stripe Processing → Email Confirmation

### Enhanced Stories (Phase 2)
- Failed payment recovery with retry mechanisms
- Payment history and reporting
- Webhook integration for asynchronous updates

## Stakeholder Decisions

### Approved Requirements
1. **Sliding Scale Implementation**: Honor-based system with no verification (CONFIRMED)
2. **Security Approach**: Stripe-only payment processing for PCI compliance (CONFIRMED)
3. **MVP Scope**: Core payment processing for event registrations only (CONFIRMED)
4. **Integration Priority**: Registration status updates critical for MVP (CONFIRMED)

### Deferred Decisions
1. **Multiple Payment Methods**: PayPal/bank transfer deferred to Phase 2
2. **Recurring Payments**: Membership fees deferred to Phase 3
3. **Payment Plans**: Complex payment plans deferred to Phase 3

## Technical Constraints Discovered

### Architecture Alignment
- **DTO Strategy**: MUST use NSwag auto-generation for all payment types (per DTO alignment strategy)
- **Vertical Slice**: Follow modern API patterns with direct Entity Framework services
- **Database Schema**: PostgreSQL integration with existing user/event tables

### External Dependencies
- **Stripe Integration**: Customer management, payment processing, webhook handling
- **Email Service**: Integration with existing notification system for receipts
- **Audit System**: Integration with existing logging infrastructure

### Performance Requirements
- **Processing Speed**: <2 seconds for payment completion
- **Success Rate**: >99% payment completion rate
- **API Response**: Payment endpoints must maintain <200ms response times

## Success Criteria for Validation

### Business Success Metrics
1. **Economic Inclusivity**: 100% preservation of sliding scale functionality
2. **Payment Success**: >99% completion rate for all transactions
3. **Security Compliance**: Zero payment security incidents
4. **User Satisfaction**: >90% satisfaction with payment experience
5. **Admin Efficiency**: 50% reduction in payment support requests

### Technical Success Metrics
1. **Performance**: <2 second average payment processing time
2. **Integration**: Seamless registration status updates
3. **Security**: PCI compliance verification
4. **Reliability**: 99.9% payment service uptime

## Data Structure Requirements (for NSwag Generation)

### Payment Entity Requirements
```csharp
// Core payment data structure needs
- PaymentId: Guid (primary key)
- RegistrationId: Guid (foreign key to registration)
- Amount: decimal (sliding scale amount)
- Currency: string (default "USD")
- Status: PaymentStatus enum (Pending, Completed, Failed, Refunded)
- PaymentMethod: string (card type/last4)
- TransactionId: string (Stripe transaction ID)
- ProcessedAt: DateTime (timestamp)
- RefundAmount: decimal? (optional for refunds)
- RefundedAt: DateTime? (refund timestamp)
```

### Business Rules for DTOs
- **Money Value Object**: Decimal precision with currency validation
- **Status Enum**: Clear payment lifecycle states
- **Audit Fields**: Created/updated timestamps required
- **Stripe Integration**: Transaction ID linking for external reference

## Risk Assessment

### High Risk Areas
1. **Payment Processing Failures**: Risk of lost revenue or broken registration flow
2. **Data Security**: PCI compliance and sensitive data protection
3. **Integration Complexity**: Multiple system coordination (payment, registration, email)

### Mitigation Strategies
1. **Comprehensive Testing**: TestContainers for payment workflow testing
2. **Security Reviews**: Regular PCI compliance audits
3. **Phased Implementation**: MVP focus reduces initial complexity
4. **Rollback Plan**: Ability to disable payment features if critical issues occur

## Questions for Next Phase

### For UI Designers
1. How should sliding scale pricing be presented to maintain dignity and privacy?
2. What error handling UX is needed for payment failures?
3. How should saved payment methods be managed in user profile?

### For Backend Developers
1. Should payment webhooks be implemented in MVP or Phase 2?
2. What level of payment retry logic is needed for failed transactions?
3. How should refund notifications be integrated with existing email system?

### For Test Developers
1. How should sliding scale calculations be tested across all percentage ranges?
2. What security testing is required for PCI compliance validation?
3. How should payment failure scenarios be tested without affecting real Stripe accounts?

## Next Actions Required

1. **UI Design**: Payment flow wireframes and sliding scale interface design
2. **Functional Specification**: Technical implementation details and API endpoint design
3. **Backend Development**: Payment entity creation and Stripe integration setup
4. **Test Planning**: Payment workflow test scenarios and security validation tests

## Files Created/Referenced

- **Business Requirements**: `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/requirements/payment-system-business-requirements.md`
- **Payment Analysis**: `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/requirements/payment-system-analysis.md`
- **Legacy Analysis**: `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/requirements/legacy-api-feature-analysis.md`

---

**CRITICAL**: This handoff represents complete greenfield payment system implementation. All subsequent work must preserve the community values of economic inclusivity through sliding scale pricing while maintaining the highest security standards for financial data processing.