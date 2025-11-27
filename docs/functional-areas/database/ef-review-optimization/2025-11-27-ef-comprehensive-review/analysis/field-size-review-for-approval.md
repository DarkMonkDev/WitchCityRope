# Field Size Recommendations - User Review Required
**Date**: 2025-11-27
**Reviewer**: Database Designer Agent
**Purpose**: Identify oversized field recommendations for user approval

## Overview

This document reviews field size recommendations from the comprehensive EF analysis. User concern: some recommended sizes may be 2x or MORE than realistically needed.

**Goal**: Get user approval on conservative vs. moderate vs. liberal sizing for each field.

---

## Email and Identity Fields

### ApplicationUser.Email
- **Current**: Unconstrained (inherits from IdentityUser)
- **Original Recommendation**: `[MaxLength(256)]`
- **User Concern**: Should be 100 max, not 250+
- **Realistic Max Examples**:
  - Shortest realistic value: "a@b.co" (6 chars)
  - Typical value: "john.smith@example.com" (23 chars)
  - Longest realistic value: "firstname.lastname.department@organization.domain.com" (60-70 chars)
  - **Actual RFC 5321 max**: 254 characters (local@domain)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(100)]` - Covers 99% of real-world emails
  - **Moderate (recommended)**: `[MaxLength(150)]` - Safe buffer for complex corporate emails
  - **Liberal (original)**: `[MaxLength(256)]` - RFC 5321 compliant
- **Analysis**: Most business emails are 30-50 chars. Conservative (100) is reasonable.
- **Recommendation**: **CONSERVATIVE (100)** - User is correct
- **Awaiting Decision**: [ ]

### ApplicationUser.EmailVerificationToken
- **Current**: text (unlimited) - EXCESSIVE
- **Original Recommendation**: `[MaxLength(100)]`
- **User Concern**: Token lengths could be shorter
- **Realistic Max Examples**:
  - GUID: "550e8400-e29b-41d4-a716-446655440000" (36 chars)
  - SHA256 hash: "64 hexadecimal characters"
  - JWT token: Variable, can be 200+ chars but we likely generate short tokens
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(50)]` - GUIDs + buffer
  - **Moderate (recommended)**: `[MaxLength(100)]` - Flexible for various token formats
  - **Liberal (original)**: `[MaxLength(200)]` - JWT support
- **Recommendation**: **CONSERVATIVE (50)** if we use GUID tokens, **MODERATE (100)** if uncertain
- **Awaiting Decision**: [ ] Need to check actual token generation code

---

## Name Fields

### ApplicationUser.FirstName (not explicitly mentioned but exists)
- **Current**: Likely unconstrained
- **Original Recommendation**: `[MaxLength(100)]`
- **User Concern**: 100 is 2x too large for names
- **Realistic Max Examples**:
  - Shortest: "Li" (2 chars)
  - Typical: "Jennifer" (8 chars)
  - Longest realistic: "Mary-Catherine-Elizabeth" (24 chars)
  - **Rare edge case**: "Hubert Blaine Wolfeschlegelsteinhausenbergerdorff" (50 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(50)]` - User recommended
  - **Moderate (recommended)**: `[MaxLength(75)]` - Handles rare long names
  - **Liberal (original)**: `[MaxLength(100)]` - Excessive
- **Recommendation**: **CONSERVATIVE (50)** - User is correct
- **Awaiting Decision**: [ ]

### ApplicationUser.LastName
- **Current**: Likely unconstrained
- **Original Recommendation**: `[MaxLength(100)]`
- **User Concern**: Same as FirstName
- **Realistic Max Examples**:
  - Shortest: "Wu" (2 chars)
  - Typical: "Johnson" (7 chars)
  - Longest realistic: "García-Fernández-Rodriguez" (28 chars)
  - **Rare edge case**: Compound surnames up to 50 chars
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(50)]` - User recommended
  - **Moderate (recommended)**: `[MaxLength(75)]` - Handles rare long names
  - **Liberal (original)**: `[MaxLength(100)]` - Excessive
- **Recommendation**: **CONSERVATIVE (50)** - User is correct
- **Awaiting Decision**: [ ]

### ApplicationUser.FullName
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(200)]`
- **User Concern**: Might be 2x too large
- **Realistic Max Examples**:
  - Shortest: "Li Wu" (5 chars)
  - Typical: "Jennifer Marie Johnson" (24 chars)
  - Longest realistic: "Mary-Catherine-Elizabeth García-Fernández-Rodriguez" (52 chars)
  - **Edge case**: FirstName(50) + space + LastName(50) = 101 chars
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(100)]` - FirstName + LastName + space
  - **Moderate (recommended)**: `[MaxLength(150)]` - Buffer for middle names/titles
  - **Liberal (original)**: `[MaxLength(200)]` - Excessive
- **Recommendation**: **CONSERVATIVE (100)** if FullName = First + Last only
- **Awaiting Decision**: [ ] Does FullName include middle names?

### ApplicationUser.RealName (deprecated)
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(200)]`
- **User Concern**: Deprecated field doesn't need generous sizing
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(100)]` - Match FullName conservative
  - **Moderate (recommended)**: `[MaxLength(150)]`
  - **Liberal (original)**: `[MaxLength(200)]`
- **Recommendation**: **CONSERVATIVE (100)** - Deprecated field, keep tight
- **Awaiting Decision**: [ ]

---

## Title and Description Fields

### Event.Title
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(200)]`
- **User Concern**: Event titles should be shorter for UI display
- **Realistic Max Examples**:
  - Shortest: "Open Rope" (9 chars)
  - Typical: "Beginner Shibari Workshop - Evening Session" (45 chars)
  - Longest realistic: "Introduction to Japanese Rope Bondage: Fundamentals and Safety - Full Day Intensive Workshop" (92 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(100)]` - Forces concise titles for UI
  - **Moderate (recommended)**: `[MaxLength(150)]` - Allows descriptive titles
  - **Liberal (original)**: `[MaxLength(200)]` - Might allow overly long titles
- **Recommendation**: **MODERATE (150)** - Balances descriptiveness with UI constraints
- **Awaiting Decision**: [ ] Check event card/grid UI width constraints

### Event.ShortDescription
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(500)]`
- **User Concern**: "Short" descriptions shouldn't be 500 chars
- **Realistic Max Examples**:
  - Shortest: "Learn basic ties" (16 chars)
  - Typical: "A hands-on workshop covering fundamental rope bondage techniques, safety protocols, and basic ties suitable for beginners." (123 chars)
  - **Comment in code says**: "~200 chars recommended for event cards"
  - Longest realistic: 250-300 chars for detailed summary
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(200)]` - Matches code comment
  - **Moderate (recommended)**: `[MaxLength(300)]` - Buffer for detailed summaries
  - **Liberal (original)**: `[MaxLength(500)]` - Too large for "short"
- **Recommendation**: **CONSERVATIVE (200)** - Code comment suggests 200
- **Awaiting Decision**: [ ]

### Event.Description (Full)
- **Current**: Unconstrained text column
- **Original Recommendation**: `[MaxLength(5000)]` or keep as text
- **User Concern**: 5000 might still be too conservative or too liberal
- **Realistic Max Examples**:
  - Shortest: 100 chars (simple event)
  - Typical: 500-1000 chars (detailed event with agenda, prerequisites, what to bring)
  - Longest realistic: 2000-3000 chars for multi-day intensive with full schedule
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(2000)]` - Most events
  - **Moderate (recommended)**: `[MaxLength(5000)]` - Complex multi-day events
  - **Liberal (original)**: Keep as `text` - Unlimited
- **Analysis**: If we constrain to 5000, we can use VARCHAR(5000) instead of text
- **Recommendation**: **MODERATE (5000)** - Reasonable upper bound
- **Awaiting Decision**: [ ] Check longest current event description

### Event.Policies
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(3000)]`
- **User Concern**: Policies could be shorter
- **Realistic Max Examples**:
  - Shortest: "Standard community guidelines apply" (35 chars)
  - Typical: Bulleted list of 5-10 policy items (500-1000 chars)
  - Longest realistic: Detailed safety rules, photo policy, conduct code (1500-2000 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(1500)]`
  - **Moderate (recommended)**: `[MaxLength(2000)]`
  - **Liberal (original)**: `[MaxLength(3000)]`
- **Recommendation**: **MODERATE (2000)** - Adequate for detailed policies
- **Awaiting Decision**: [ ]

---

## Session and Code Fields

### Session.SessionCode
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(20)]`
- **User Concern**: Session codes could be much shorter
- **Realistic Max Examples**:
  - Shortest: "S1" (2 chars)
  - Typical: "DAY1-AM", "SESSION-A" (6-9 chars)
  - Longest realistic: "2025-WORKSHOP-01-MORNING" (24 chars) - but unlikely
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(10)]` - Most use cases
  - **Moderate (recommended)**: `[MaxLength(20)]` - Flexible
  - **Liberal (original)**: `[MaxLength(50)]` - Excessive
- **Recommendation**: **CONSERVATIVE (10)** or **MODERATE (20)**
- **Awaiting Decision**: [ ] Check actual session code format

### Session.Name
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(200)]`
- **User Concern**: Session names should be shorter than event titles
- **Realistic Max Examples**:
  - Shortest: "Morning Session" (15 chars)
  - Typical: "Day 1 - Introduction to Rope Bondage" (37 chars)
  - Longest realistic: "Saturday Morning Intensive: Advanced Suspension Techniques and Safety" (70 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(100)]`
  - **Moderate (recommended)**: `[MaxLength(150)]`
  - **Liberal (original)**: `[MaxLength(200)]`
- **Recommendation**: **CONSERVATIVE (100)** - Sessions shorter than events
- **Awaiting Decision**: [ ]

---

## Payment and Financial Fields

### Payment.Currency / PaymentRefund.RefundCurrency
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(3)]`
- **User Concern**: 3 is correct for ISO 4217
- **Realistic Max Examples**:
  - **ALL values**: Exactly 3 characters (USD, EUR, GBP, JPY, CAD)
  - **ISO 4217 standard**: 3-letter codes only
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(3)]` - EXACT
  - **Moderate**: Same
  - **Liberal**: Same
- **Recommendation**: **CONSERVATIVE (3)** - User confirmed correct
- **Awaiting Decision**: [X] APPROVED - 3 is correct

### TicketPurchase.PaymentStatus
- **Current**: Unconstrained string
- **Original Recommendation**: `[MaxLength(50)]`
- **User Concern**: Should be enum, but if string, 50 might be large
- **Realistic Max Examples**:
  - **Actual values**: "Pending" (7), "Completed" (9), "Refunded" (8), "Failed" (6), "Cancelled" (9)
  - **Longest**: "PartiallyRefunded" (17 chars) if we use this
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(20)]` - Covers all status values
  - **Moderate (recommended)**: `[MaxLength(30)]` - Buffer
  - **Liberal (original)**: `[MaxLength(50)]` - Excessive
- **Long-term**: Convert to enum
- **Recommendation**: **CONSERVATIVE (20)** for now
- **Awaiting Decision**: [ ] Should we convert to enum instead?

### TicketPurchase.PaymentMethod
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(50)]`
- **User Concern**: Payment methods are short strings
- **Realistic Max Examples**:
  - **Actual values**: "PayPal" (6), "Stripe" (6), "Cash" (4), "Venmo" (5), "CreditCard" (10)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(20)]`
  - **Moderate (recommended)**: `[MaxLength(30)]`
  - **Liberal (original)**: `[MaxLength(50)]`
- **Recommendation**: **CONSERVATIVE (20)**
- **Awaiting Decision**: [ ]

### TicketPurchase.PaymentReference
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(100)]`
- **User Concern**: External transaction IDs could be shorter
- **Realistic Max Examples**:
  - PayPal transaction ID: ~17 chars ("1AB23456C78910DEF")
  - Stripe payment ID: ~27 chars ("pi_1A2B3C4D5E6F7G8H9I0J1K")
  - **Longest realistic**: 50 chars for various payment processors
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(50)]`
  - **Moderate (recommended)**: `[MaxLength(100)]` - Safe for unknown processors
  - **Liberal (original)**: `[MaxLength(200)]`
- **Recommendation**: **MODERATE (100)** - Multiple payment processors
- **Awaiting Decision**: [ ] Check PayPal/Stripe actual ID lengths

### Payment.VenmoUsername
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(100)]`
- **User Concern**: Venmo usernames have platform limits
- **Realistic Max Examples**:
  - **Venmo actual limit**: 16 characters for usernames
  - **Typical**: "@john-smith" (11 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(20)]` - Venmo limit + buffer
  - **Moderate (recommended)**: `[MaxLength(50)]`
  - **Liberal (original)**: `[MaxLength(100)]`
- **Recommendation**: **CONSERVATIVE (20)** - Venmo has 16 char limit
- **Awaiting Decision**: [ ] Verify Venmo username length limit

---

## Notes and Text Fields

### TicketPurchase.Notes
- **Current**: Check constraint max 2000 characters
- **Original Recommendation**: `[MaxLength(2000)]` to match constraint
- **User Concern**: 2000 might be too large for purchase notes
- **Realistic Max Examples**:
  - Shortest: Empty or "Regular purchase" (16 chars)
  - Typical: "Scholarship recipient - 50% discount approved by admin" (55 chars)
  - Longest realistic: Detailed admin notes about special circumstances (300-500 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(500)]`
  - **Moderate (recommended)**: `[MaxLength(1000)]`
  - **Liberal (original)**: `[MaxLength(2000)]` - Database constraint
- **Analysis**: Database constraint is 2000, so we're limited by that
- **Recommendation**: **MODERATE (1000)** - Adequate for admin notes
- **Awaiting Decision**: [ ] Change database constraint too?

### ApplicationUser.Bio
- **Current**: text column (unlimited)
- **Original Recommendation**: `[MaxLength(2000)]`
- **User Concern**: User bios should be shorter
- **Realistic Max Examples**:
  - Shortest: "New to rope" (11 chars)
  - Typical: 2-3 paragraph bio (300-500 chars)
  - Longest realistic: Detailed background, experience, interests (1000-1500 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(500)]` - Twitter-length bio
  - **Moderate (recommended)**: `[MaxLength(1000)]` - Detailed but focused
  - **Liberal (original)**: `[MaxLength(2000)]` - Very detailed
- **Recommendation**: **MODERATE (1000)** - Encourages concise bios
- **Awaiting Decision**: [ ] Check UI display constraints for user profiles

### VolunteerPosition.Description
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(1000)]`
- **User Concern**: Role descriptions could be shorter
- **Realistic Max Examples**:
  - Shortest: "Help set up equipment" (21 chars)
  - Typical: Bulleted list of 3-5 responsibilities (200-400 chars)
  - Longest realistic: Detailed role with requirements, schedule, expectations (600-800 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(500)]`
  - **Moderate (recommended)**: `[MaxLength(1000)]`
  - **Liberal (original)**: `[MaxLength(2000)]`
- **Recommendation**: **CONSERVATIVE (500)** - Keep role descriptions focused
- **Awaiting Decision**: [ ]

### Payment.RefundReason
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(500)]`
- **User Concern**: Refund reasons should be concise
- **Realistic Max Examples**:
  - Shortest: "Event cancelled" (15 chars)
  - Typical: "User requested refund due to scheduling conflict" (49 chars)
  - Longest realistic: Detailed explanation of complex refund situation (200-300 chars)
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(200)]`
  - **Moderate (recommended)**: `[MaxLength(500)]`
  - **Liberal (original)**: `[MaxLength(1000)]`
- **Recommendation**: **CONSERVATIVE (200)** - Concise explanations
- **Awaiting Decision**: [ ]

---

## Phone Number Fields

### ApplicationUser.PhoneNumber (if exists)
- **Current**: Unknown (inherited from Identity?)
- **Original Recommendation**: Not explicitly mentioned in analysis
- **User Concern**: Phone numbers should be 20 max with international formatting
- **Realistic Max Examples**:
  - Shortest: "5551234567" (10 chars - US)
  - Typical: "+1-555-123-4567" (15 chars)
  - Longest realistic: "+44 20 7123 4567 ext. 1234" (26 chars with extension)
  - **E.164 format**: +[country code][number] = max 15 digits
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(15)]` - E.164 standard
  - **Moderate (recommended)**: `[MaxLength(20)]` - User recommended (E.164 + formatting)
  - **Liberal (original)**: `[MaxLength(30)]` - Extensions
- **Recommendation**: **MODERATE (20)** - User is correct
- **Awaiting Decision**: [ ] Verify we have phone number field

---

## Token and Code Fields

### VettingApplication.ApplicationNumber
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(30)]`
- **User Concern**: Format suggests shorter
- **Realistic Max Examples**:
  - **Format**: "VET-YYYYMMDD-XXXXX" = "VET-20251127-00001" (19 chars)
  - **Buffer**: 30 provides 11 chars extra
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(25)]` - Format + buffer
  - **Moderate (recommended)**: `[MaxLength(30)]`
  - **Liberal (original)**: `[MaxLength(50)]`
- **Recommendation**: **CONSERVATIVE (25)** - Tight to format
- **Awaiting Decision**: [ ] Confirm application number format

### VettingApplication.StatusToken
- **Current**: Unconstrained
- **Original Recommendation**: `[MaxLength(50)]`
- **User Concern**: Tokens could be shorter
- **Realistic Max Examples**:
  - GUID: 36 chars
  - Short hash: 32 chars
- **Proposed Options**:
  - **Conservative (tight)**: `[MaxLength(40)]` - GUID + buffer
  - **Moderate (recommended)**: `[MaxLength(50)]`
  - **Liberal (original)**: `[MaxLength(100)]`
- **Recommendation**: **CONSERVATIVE (40)**
- **Awaiting Decision**: [ ] Check token generation code

---

## Summary Statistics

### Fields Needing User Decision: 25 fields

### Categorized by Confidence Level:

**HIGH CONFIDENCE - User Clearly Correct**:
- Email: 100 not 256 ✓
- FirstName/LastName: 50 not 100 ✓
- Currency: 3 (exact) ✓
- Phone: 20 with formatting ✓
- ShortDescription: 200 not 500 ✓

**MEDIUM CONFIDENCE - Likely Correct**:
- Event.Title: 150 instead of 200
- FullName: 100 instead of 200
- Session.Name: 100 instead of 200
- PaymentStatus/PaymentMethod: 20 instead of 50
- VenmoUsername: 20 instead of 100

**NEEDS INVESTIGATION**:
- EmailVerificationToken: Need to check token generation
- PaymentReference: Need to check actual processor IDs
- Bio: Depends on UI constraints
- Notes fields: Various - depend on use cases

---

## Recommended Actions

1. **APPROVE** fields marked "User Clearly Correct" (5 fields)
2. **REVIEW** Medium Confidence fields with conservative recommendations (10 fields)
3. **INVESTIGATE** fields that need code/data analysis (10 fields)

---

## Next Steps

1. User reviews this document
2. User approves/adjusts sizing for each field
3. Create migration with approved sizes
4. Test on development database
5. Validate no existing data violates new constraints
6. Deploy to staging then production

---

**END OF FIELD SIZE REVIEW DOCUMENT**
