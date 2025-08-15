# Database Seed Data - WitchCityRope

This document contains information about the default seed data created when the application starts.

## Default User Accounts

All default accounts use the password: `Test123!`

| Email | Scene Name | Role | Status | Notes |
|-------|------------|------|--------|-------|
| admin@witchcityrope.com | Admin | Administrator | Active | Full system access |
| staff@witchcityrope.com | StaffMember | Moderator | Active | Event management, user moderation |
| member@witchcityrope.com | RopeLover | Member | Active/Vetted | Vetted member with full access |
| guest@witchcityrope.com | CuriousGuest | Guest | Active | Basic attendee access |
| organizer@witchcityrope.com | EventOrganizer | Moderator | Active | Event creation and management |

## Login URLs

### Web Application
- **Login Page**: https://localhost:5652/login or https://localhost:5652/auth/login
- **Admin Dashboard**: https://localhost:5652/admin (requires Admin role)
- **Member Dashboard**: https://localhost:5652/dashboard or https://localhost:5652/member/dashboard
- **Events Page**: https://localhost:5652/events

### API Endpoints
- **Login Endpoint**: `POST https://localhost:5654/api/v1/auth/login`
  ```json
  {
    "email": "admin@witchcityrope.com",
    "password": "Test123!"
  }
  ```

## Seeded Events

The database is populated with 12 sample events:

### Upcoming Events (10)

1. **Introduction to Rope Safety**
   - Date: 5 days from now
   - Time: 2:00 PM - 4:00 PM
   - Price: $45 (sliding scale: $35/$25)
   - Capacity: 30 (12 registered)
   - Level: Beginner

2. **March Rope Jam** (Members Only)
   - Date: 8 days from now
   - Time: 7:00 PM - 10:00 PM
   - Price: $15 (sliding scale: $10/$5)
   - Capacity: 60 (28 registered)
   - Level: All Levels
   - Requires vetting

3. **Suspension Intensive Workshop**
   - Date: 12 days from now
   - Time: 1:00 PM - 6:00 PM
   - Price: $95 (early bird: $85, sliding scale: $75)
   - Capacity: 20 (18 registered - almost full!)
   - Level: Advanced
   - Prerequisites required

4. **Rope and Sensation Play**
   - Date: 15 days from now
   - Time: 2:00 PM - 5:00 PM
   - Price: $55 (sliding scale: $40)
   - Capacity: 24 (10 registered)
   - Level: Intermediate

5. **Rope Fundamentals: Floor Work**
   - Date: 18 days from now
   - Time: 2:00 PM - 4:00 PM
   - Price: $45 (sliding scale: $35/$25)
   - Capacity: 30 (8 registered)
   - Level: Beginner

6. **Dynamic Suspension: Movement and Flow** (Vetted Members Only)
   - Date: 22 days from now
   - Time: 1:00 PM - 6:00 PM
   - Price: $125 (early bird: $110)
   - Capacity: 16 (14 registered - almost full!)
   - Level: Advanced
   - Requires vetting

7. **Monthly Rope Social** (FREE)
   - Date: 25 days from now
   - Time: 6:00 PM - 9:00 PM
   - Price: FREE
   - Capacity: 80 (35 registered)
   - Level: All Levels

8. **Rope Play Party** (Vetted Members Only, 21+)
   - Date: 28 days from now
   - Time: 8:00 PM - 2:00 AM
   - Price: $25 (sliding scale: $15)
   - Capacity: 50 (32 registered)
   - Level: All Levels
   - Requires vetting

9. **Virtual Rope Workshop: Self-Tying**
   - Date: 30 days from now
   - Time: 7:00 PM - 9:00 PM (Online)
   - Price: $25 (sliding scale: $15)
   - Capacity: 100 (45 registered)
   - Level: Beginner

10. **New England Rope Intensive** (3-Day Conference)
    - Date: 45 days from now
    - Price: $250 (early bird: $200, single day: $100)
    - Capacity: 200 (156 registered)
    - Level: All Levels

### Past Events (2)

1. **February Rope Jam**
   - Date: 10 days ago
   - Attendance: 52/60
   - Status: Completed

2. **Valentine's Rope Workshop**
   - Date: 15 days ago
   - Attendance: 20/20 (Sold Out)
   - Status: Completed

## Testing Different User Scenarios

### As Admin (admin@witchcityrope.com)
- Full access to admin dashboard
- Can manage all users
- Can create/edit/delete any event
- Can view financial reports
- Can manage incident reports
- Can approve vetting applications

### As Staff/Moderator (staff@witchcityrope.com)
- Access to limited admin features
- Can manage events
- Can moderate users
- Cannot view financial reports
- Can handle incident reports

### As Vetted Member (member@witchcityrope.com)
- Can register for all events including member-only
- Can view member dashboard
- Can update profile
- Can view past registrations
- Can access member resources

### As Guest (guest@witchcityrope.com)
- Can only register for public events
- Cannot access member-only events
- Limited dashboard features
- Cannot access vetted content

### As Event Organizer (organizer@witchcityrope.com)
- Can create and manage events
- Can view event registrations
- Can send event updates
- Limited admin access

## API Testing Examples

### Login Request
```bash
curl -X POST https://localhost:5654/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@witchcityrope.com",
    "password": "Test123!"
  }'
```

### Get Events (Authenticated)
```bash
curl -X GET https://localhost:5654/api/v1/events \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Register for Event
```bash
curl -X POST https://localhost:5654/api/v1/events/{eventId}/register \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "priceTierId": "full-price"
  }'
```

## Notes

- All users start with verified emails for immediate testing
- The member@witchcityrope.com account is pre-vetted
- Events have realistic attendance numbers for testing capacity limits
- Sliding scale pricing is implemented on all paid events
- Some events are marked as "almost full" to test UI states
- Past events are included to test historical data display