# Database Test Accounts Verification

## Current Test Accounts in DbInitializer

The following test accounts are properly configured in the database seeder without any "mailto:" prefix:

### 1. Administrator Account
- **Email**: admin@witchcityrope.com
- **Password**: Test123!
- **Scene Name**: Admin
- **Role**: Administrator

### 2. Staff Account  
- **Email**: staff@witchcityrope.com
- **Password**: Test123!
- **Scene Name**: StaffMember
- **Role**: Moderator

### 3. Member Account
- **Email**: member@witchcityrope.com
- **Password**: Test123!
- **Scene Name**: RopeLover
- **Role**: Member

### 4. Guest Account
- **Email**: guest@witchcityrope.com
- **Password**: Test123!
- **Scene Name**: CuriousGuest
- **Role**: Attendee

### 5. Organizer Account
- **Email**: organizer@witchcityrope.com
- **Password**: Test123!
- **Scene Name**: EventOrganizer
- **Role**: Moderator

## Database Seeding Process

The DbInitializer properly:
1. Creates users with the correct email addresses (no mailto: prefix)
2. Hashes passwords using BCrypt
3. Creates authentication records for each user
4. Checks for existing users to avoid duplicates
5. Seeds sample events and vetting applications

## Potential "mailto:" Issue

If you're seeing "mailto:" in the UI, it could be from:

1. **Browser Auto-linking**: Some browsers automatically convert email addresses to mailto links
2. **CSS Framework**: A CSS framework might be styling email addresses
3. **Blazor Component**: A component might be auto-formatting emails
4. **Display Logic**: The UI might be adding mailto: for clickable emails

## Recommendation

The database is correctly configured. If "mailto:" appears in the UI:
1. Check the MainLayout.razor line 89 where email is displayed
2. Consider using just the username part of email for display
3. Or explicitly format the display to remove any auto-linking