# WitchCityRope Database Seed Data & Login Documentation

## Default User Accounts

All default accounts use the password: **`Test123!`**

### 1. Admin Account
- **Email:** admin@witchcityrope.com
- **Scene Name:** RopeMaster
- **Role:** Administrator
- **Password:** Test123!
- **Features:** Full system access, user management, event management, financial reports

### 2. Staff Account (Moderator)
- **Email:** staff@witchcityrope.com
- **Scene Name:** SafetyFirst
- **Role:** Moderator
- **Password:** Test123!
- **Features:** Safety team access, incident management, vetting review

### 3. Member Account (Vetted)
- **Email:** member@witchcityrope.com
- **Scene Name:** RopeEnthusiast
- **Role:** Member (Vetted)
- **Password:** Test123!
- **Features:** Access to member-only events, can register for advanced classes

### 4. Guest Account (Attendee)
- **Email:** guest@witchcityrope.com
- **Scene Name:** Newcomer
- **Role:** Attendee
- **Password:** Test123!
- **Features:** Basic access, can register for beginner events

### 5. Event Organizer Account
- **Email:** organizer@witchcityrope.com
- **Scene Name:** EventMaker
- **Role:** Organizer
- **Password:** Test123!
- **Features:** Can create and manage events

## Login URLs & Routes

### Web Application (Blazor)
- **Main Login Page:** `/auth/login`
- **Direct URL:** https://localhost:5653/auth/login (or your configured port)

### Dashboard URLs (After Login)
- **Admin Dashboard:** `/admin/dashboard` or `/admin`
- **Member Dashboard:** `/dashboard`
- **Profile Page:** `/profile`
- **Events Page:** `/events`

### API Endpoints
- **Base API URL:** https://localhost:5654 (or your configured port)
- **Login Endpoint:** `POST /api/v1/auth/login`
- **Register Endpoint:** `POST /api/v1/auth/register`
- **Refresh Token:** `POST /api/v1/auth/refresh`

## Seeded Events Data

The database is populated with 12 default events:

### Beginner Events
1. **Introduction to Rope Safety**
   - When: 7 days from now, 6:00 PM - 9:00 PM
   - Location: Main Dungeon Space
   - Capacity: 20 people
   - Price: $25-$45 (sliding scale)

2. **Beginner's Rope Jam**
   - When: 10 days from now, 7:00 PM - 10:00 PM
   - Location: Community Hall
   - Capacity: 30 people
   - Price: $10-$20 (sliding scale)

### Intermediate Events
3. **Intermediate Suspension Techniques**
   - When: 14 days from now, 6:00 PM - 9:00 PM
   - Location: Main Dungeon Space
   - Capacity: 12 people
   - Price: $50-$80 (sliding scale)

4. **Rope and Sensation Play**
   - When: 21 days from now, 7:00 PM - 10:00 PM
   - Location: Workshop Room A
   - Capacity: 16 people
   - Price: $40-$60 (sliding scale)

### Advanced Events
5. **Advanced Dynamic Suspension**
   - When: 28 days from now, 10:00 AM - 6:00 PM
   - Location: Main Dungeon Space
   - Capacity: 8 people
   - Price: $100-$150 (sliding scale)
   - Note: Vetting required

### Social Events
6. **Monthly Rope Social**
   - When: 15 days from now, 7:00 PM - 10:00 PM
   - Location: Community Hall
   - Capacity: 50 people
   - Price: $0-$10 (donation-based)

### Special Events
7. **Rope Play Party - Summer Solstice**
   - When: 35 days from now, 8:00 PM - 2:00 AM
   - Location: Main Venue - All Spaces
   - Capacity: 75 people
   - Price: $30-$50 (sliding scale)
   - Note: Vetting required

8. **Online: Self-Tying Techniques**
   - When: 5 days from now, 7:00 PM - 9:00 PM
   - Location: Online - Zoom Link Provided
   - Capacity: 100 people
   - Price: $15-$25 (sliding scale)

9. **Rope Art Performance Night**
   - When: 30 days from now, 7:00 PM - 10:00 PM
   - Location: Theater Space
   - Capacity: 60 people
   - Price: $20-$40 (sliding scale)

10. **New England Rope Intensive**
    - When: 60-62 days from now (3-day conference)
    - Location: Convention Center
    - Capacity: 150 people
    - Price: $200-$300 (sliding scale)

### Past Events (for testing)
11. **Spring Rope Basics (Past)**
    - When: 30 days ago
    - Location: Workshop Room B
    - Capacity: 20 people

12. **Valentine's Rope Social (Past)**
    - When: 45 days ago
    - Location: Community Hall
    - Capacity: 40 people

## How to Initialize the Database

The database is automatically initialized when the API starts up. The seeding process:

1. Applies any pending migrations
2. Creates default user accounts with authentication data
3. Creates sample events with various types and price points
4. All events are published and ready for registration

## Testing Different User Experiences

### As Admin (admin@witchcityrope.com)
- Full access to admin dashboard at `/admin`
- Can manage users, events, and view financial reports
- Can access safety team features

### As Staff (staff@witchcityrope.com)
- Access to moderation features
- Can review vetting applications
- Can manage incident reports

### As Member (member@witchcityrope.com)
- Vetted status allows registration for advanced events
- Access to member dashboard at `/dashboard`
- Can view and register for all event types

### As Guest (guest@witchcityrope.com)
- Limited to beginner events and public content
- Cannot register for events requiring vetting
- Basic dashboard access

### As Organizer (organizer@witchcityrope.com)
- Can create new events
- Manage existing events they organize
- View registration lists for their events

## API Testing with Default Accounts

### Login Request Example
```bash
curl -X POST https://localhost:5654/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@witchcityrope.com",
    "password": "Test123!"
  }'
```

### Expected Response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresAt": "2025-06-30T12:00:00Z",
  "user": {
    "id": "user-guid-here",
    "email": "admin@witchcityrope.com",
    "sceneName": "RopeMaster",
    "role": "Administrator",
    "isActive": true
  }
}
```

## Notes

- All users are created with verified email addresses for immediate testing
- The encryption service is used to securely store legal names
- Passwords are hashed using BCrypt
- Events use sliding scale pricing with 3 tiers each
- The database uses SQLite by default (configured in appsettings.json)