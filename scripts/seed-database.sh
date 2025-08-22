#!/bin/bash

# Script to seed the WitchCityRope database with test data

echo "Seeding WitchCityRope database..."

# Database connection details
DB_HOST="localhost"
DB_PORT="5433"
DB_NAME="witchcityrope_db"
DB_USER="postgres"
DB_PASSWORD="WitchCity2024!"

# Export password for psql
export PGPASSWORD=$DB_PASSWORD

# Check if database exists and has tables
TABLE_COUNT=$(docker exec witchcity-postgres psql -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name != '__EFMigrationsHistory';")

if [ $TABLE_COUNT -eq 0 ]; then
    echo "Error: No tables found in database. Please run migrations first."
    exit 1
fi

# Check if data already exists
USER_COUNT=$(docker exec witchcity-postgres psql -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM \"Users\";")

if [ $USER_COUNT -gt 0 ]; then
    echo "Database already contains data. Skipping seed."
    exit 0
fi

echo "Seeding users..."

# First, let's create the users
docker exec witchcity-postgres psql -U $DB_USER -d $DB_NAME << 'EOF'
-- Admin User
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "PhoneNumber", "EmergencyContactName", "EmergencyContactPhone", "MembershipLevel", "VettingStatus", "LastLoginAt", "LoginCount", "IsActive", "IsBanned", "BanReason", "BanExpiresAt", "Notes", "ProfilePhotoUrl", "Bio", "FetlifeUsername", "DiscordUsername", "TelegramUsername", "ReferralSource", "ReferralDetails", "ConsentToPhotography", "ConsentToNewsletter", "PreferredCommunicationMethod", "CommunicationPreferences", "EventNotificationPreference", "MessageNotificationPreference", "ReminderNotificationPreference") 
VALUES 
(gen_random_uuid(), 'QWRtaW4gVXNlcg==', 'Admin', 'admin@witchcityrope.com', '1990-01-01', 3, NOW(), NOW(), '', '', '', '', '', 0, 0, NULL, 0, true, false, '', NULL, '', '', '', '', '', '', '', '', false, false, 0, '', 0, 0, 0);

-- Teacher User
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "PhoneNumber", "EmergencyContactName", "EmergencyContactPhone", "MembershipLevel", "VettingStatus", "LastLoginAt", "LoginCount", "IsActive", "IsBanned", "BanReason", "BanExpiresAt", "Notes", "ProfilePhotoUrl", "Bio", "FetlifeUsername", "DiscordUsername", "TelegramUsername", "ReferralSource", "ReferralDetails", "ConsentToPhotography", "ConsentToNewsletter", "PreferredCommunicationMethod", "CommunicationPreferences", "EventNotificationPreference", "MessageNotificationPreference", "ReminderNotificationPreference") 
VALUES 
(gen_random_uuid(), 'VGVhY2hlciBVc2Vy', 'TeacherRope', 'teacher@witchcityrope.com', '1988-05-15', 2, NOW(), NOW(), '', '', '', '', '', 0, 2, NULL, 0, true, false, '', NULL, '', '', '', '', '', '', '', '', false, false, 0, '', 0, 0, 0);

-- Vetted Member
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "PhoneNumber", "EmergencyContactName", "EmergencyContactPhone", "MembershipLevel", "VettingStatus", "LastLoginAt", "LoginCount", "IsActive", "IsBanned", "BanReason", "BanExpiresAt", "Notes", "ProfilePhotoUrl", "Bio", "FetlifeUsername", "DiscordUsername", "TelegramUsername", "ReferralSource", "ReferralDetails", "ConsentToPhotography", "ConsentToNewsletter", "PreferredCommunicationMethod", "CommunicationPreferences", "EventNotificationPreference", "MessageNotificationPreference", "ReminderNotificationPreference") 
VALUES 
(gen_random_uuid(), 'VmV0dGVkIE1lbWJlcg==', 'RopeExplorer', 'vetted@witchcityrope.com', '1995-08-20', 1, NOW(), NOW(), '', '', '', '', '', 0, 2, NULL, 0, true, false, '', NULL, '', '', '', '', '', '', '', '', false, false, 0, '', 0, 0, 0);

-- General Member
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "PhoneNumber", "EmergencyContactName", "EmergencyContactPhone", "MembershipLevel", "VettingStatus", "LastLoginAt", "LoginCount", "IsActive", "IsBanned", "BanReason", "BanExpiresAt", "Notes", "ProfilePhotoUrl", "Bio", "FetlifeUsername", "DiscordUsername", "TelegramUsername", "ReferralSource", "ReferralDetails", "ConsentToPhotography", "ConsentToNewsletter", "PreferredCommunicationMethod", "CommunicationPreferences", "EventNotificationPreference", "MessageNotificationPreference", "ReminderNotificationPreference") 
VALUES 
(gen_random_uuid(), 'R2VuZXJhbCBNZW1iZXI=', 'CuriousNewbie', 'member@witchcityrope.com', '2000-03-10', 0, NOW(), NOW(), '', '', '', '', '', 0, 0, NULL, 0, true, false, '', NULL, '', '', '', '', '', '', '', '', false, false, 0, '', 0, 0, 0);
EOF

echo "Adding authentication records..."

# Now add authentication records with hashed passwords (Test123!)
docker exec witchcity-postgres psql -U $DB_USER -d $DB_NAME << 'EOF'
-- Password hash for "Test123!" using BCrypt
INSERT INTO "UserAuthentications" ("Id", "UserId", "PasswordHash", "SecurityStamp", "TwoFactorSecret", "IsTwoFactorEnabled", "TwoFactorRecoveryCodes", "FailedLoginAttempts", "LockoutEnd", "IsLockedOut", "LastPasswordChangeDate", "PasswordResetToken", "PasswordResetTokenExpiry", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "IsEmailConfirmed", "PhoneConfirmationToken", "PhoneConfirmationTokenExpiry", "IsPhoneConfirmed", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    u."Id",
    '$2a$11$Xx0qG7W7k1SBfI7QMJvpuOuLqk5VPw2mLcGhLrZQLQbOi.mJBZtO6', -- BCrypt hash of "Test123!"
    gen_random_uuid()::text,
    NULL,
    false,
    NULL,
    0,
    NULL,
    false,
    NOW(),
    NULL,
    NULL,
    NULL,
    NULL,
    true,
    NULL,
    NULL,
    false,
    NOW(),
    NOW()
FROM "Users" u;
EOF

echo "Seeding events..."

# Get user IDs for event creation
ADMIN_ID=$(docker exec witchcity-postgres psql -U $DB_USER -d $DB_NAME -t -c "SELECT \"Id\" FROM \"Users\" WHERE \"Email\" = 'admin@witchcityrope.com';")
TEACHER_ID=$(docker exec witchcity-postgres psql -U $DB_USER -d $DB_NAME -t -c "SELECT \"Id\" FROM \"Users\" WHERE \"Email\" = 'teacher@witchcityrope.com';")

# Seed events
docker exec witchcity-postgres psql -U $DB_USER -d $DB_NAME << EOF
-- Introduction to Rope Safety
INSERT INTO "Events" ("Id", "Title", "Description", "ImageUrl", "EventType", "Status", "StartDate", "EndDate", "Location", "VirtualMeetingUrl", "Capacity", "WaitlistCapacity", "AvailableSpots", "IsPrivate", "RequiresVetting", "MinimumAge", "SkillLevel", "DressCode", "EquipmentProvided", "EquipmentToBring", "Prerequisites", "CancellationPolicy", "Tags", "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt", "CancelledAt", "CancellationReason", "VenueName", "VenueAddress", "VenueCity", "VenueState", "VenueZipCode", "VenueCountry", "VenueAccessibilityInfo", "ParkingInfo", "PublicTransportInfo", "CheckInTime", "DoorsCloseTime", "LastEntryTime", "FeaturedUntil", "PricingTiers", "EarlyBirdEndDate", "MemberDiscount", "MultiEventDiscount", "IsRecurring", "RecurrencePattern", "RecurrenceEndDate", "ParentEventId")
VALUES
(gen_random_uuid(), 'Introduction to Rope Safety', 'Perfect for beginners! Learn the fundamentals of rope safety, basic knots, and communication techniques in a supportive environment.', NULL, 1, 1, NOW() + INTERVAL '5 days' + INTERVAL '14 hours', NOW() + INTERVAL '5 days' + INTERVAL '16 hours', 'The Rope Space - Main Room', NULL, 30, 10, 30, false, false, 18, 0, NULL, true, 'Comfortable clothing, water bottle', NULL, '24 hour cancellation policy', '["beginner","safety","workshop"]', '$ADMIN_ID'::uuid, NOW(), NOW(), NOW(), NULL, NULL, 'The Rope Space', '123 Main St', 'Salem', 'MA', '01970', 'USA', 'Wheelchair accessible', 'Street parking available', 'Bus stop 2 blocks away', NOW() + INTERVAL '5 days' + INTERVAL '13 hours' + INTERVAL '45 minutes', NULL, NOW() + INTERVAL '5 days' + INTERVAL '14 hours' + INTERVAL '15 minutes', NULL, '[{"amount":45.00,"currency":"USD"},{"amount":35.00,"currency":"USD"},{"amount":25.00,"currency":"USD"}]'::jsonb, NULL, 10.0, NULL, false, NULL, NULL, NULL);

-- March Rope Jam
INSERT INTO "Events" ("Id", "Title", "Description", "ImageUrl", "EventType", "Status", "StartDate", "EndDate", "Location", "VirtualMeetingUrl", "Capacity", "WaitlistCapacity", "AvailableSpots", "IsPrivate", "RequiresVetting", "MinimumAge", "SkillLevel", "DressCode", "EquipmentProvided", "EquipmentToBring", "Prerequisites", "CancellationPolicy", "Tags", "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt", "CancelledAt", "CancellationReason", "VenueName", "VenueAddress", "VenueCity", "VenueState", "VenueZipCode", "VenueCountry", "VenueAccessibilityInfo", "ParkingInfo", "PublicTransportInfo", "CheckInTime", "DoorsCloseTime", "LastEntryTime", "FeaturedUntil", "PricingTiers", "EarlyBirdEndDate", "MemberDiscount", "MultiEventDiscount", "IsRecurring", "RecurrencePattern", "RecurrenceEndDate", "ParentEventId")
VALUES
(gen_random_uuid(), 'March Rope Jam', 'Monthly practice space for vetted members. Bring your rope and practice partners for a social evening of rope bondage in a safe, monitored environment.', NULL, 3, 1, NOW() + INTERVAL '8 days' + INTERVAL '19 hours', NOW() + INTERVAL '8 days' + INTERVAL '22 hours', 'The Rope Space - All Rooms', NULL, 60, 20, 60, false, true, 18, 1, NULL, false, 'Your own rope', 'Must be vetted member', '24 hour cancellation policy', '["social","practice","vetted-only"]', '$TEACHER_ID'::uuid, NOW(), NOW(), NOW(), NULL, NULL, 'The Rope Space', '123 Main St', 'Salem', 'MA', '01970', 'USA', 'Wheelchair accessible', 'Street parking available', 'Bus stop 2 blocks away', NOW() + INTERVAL '8 days' + INTERVAL '18 hours' + INTERVAL '45 minutes', NULL, NOW() + INTERVAL '8 days' + INTERVAL '19 hours' + INTERVAL '30 minutes', NULL, '[{"amount":15.00,"currency":"USD"},{"amount":10.00,"currency":"USD"},{"amount":5.00,"currency":"USD"}]'::jsonb, NULL, 20.0, NULL, true, 'Monthly', NOW() + INTERVAL '365 days', NULL);

-- Suspension Intensive Workshop
INSERT INTO "Events" ("Id", "Title", "Description", "ImageUrl", "EventType", "Status", "StartDate", "EndDate", "Location", "VirtualMeetingUrl", "Capacity", "WaitlistCapacity", "AvailableSpots", "IsPrivate", "RequiresVetting", "MinimumAge", "SkillLevel", "DressCode", "EquipmentProvided", "EquipmentToBring", "Prerequisites", "CancellationPolicy", "Tags", "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt", "CancelledAt", "CancellationReason", "VenueName", "VenueAddress", "VenueCity", "VenueState", "VenueZipCode", "VenueCountry", "VenueAccessibilityInfo", "ParkingInfo", "PublicTransportInfo", "CheckInTime", "DoorsCloseTime", "LastEntryTime", "FeaturedUntil", "PricingTiers", "EarlyBirdEndDate", "MemberDiscount", "MultiEventDiscount", "IsRecurring", "RecurrencePattern", "RecurrenceEndDate", "ParentEventId")
VALUES
(gen_random_uuid(), 'Suspension Intensive Workshop', 'Take your skills to new heights! This intensive workshop covers suspension basics, safety protocols, and hands-on practice with experienced instructors.', NULL, 1, 1, NOW() + INTERVAL '12 days' + INTERVAL '13 hours', NOW() + INTERVAL '12 days' + INTERVAL '18 hours', 'The Rope Space - Main Room', NULL, 20, 5, 20, false, false, 18, 2, NULL, true, 'Your own suspension-rated rope, carabiners', '6 months rope experience required', '48 hour cancellation policy', '["advanced","suspension","workshop","intensive"]', '$ADMIN_ID'::uuid, NOW(), NOW(), NOW(), NULL, NULL, 'The Rope Space', '123 Main St', 'Salem', 'MA', '01970', 'USA', 'Wheelchair accessible', 'Street parking available', 'Bus stop 2 blocks away', NOW() + INTERVAL '12 days' + INTERVAL '12 hours' + INTERVAL '30 minutes', NULL, NOW() + INTERVAL '12 days' + INTERVAL '13 hours' + INTERVAL '15 minutes', NOW() + INTERVAL '7 days', '[{"amount":95.00,"currency":"USD"},{"amount":85.00,"currency":"USD"},{"amount":75.00,"currency":"USD"}]'::jsonb, NOW() + INTERVAL '7 days', 15.0, NULL, false, NULL, NULL, NULL);

-- Monthly Rope Social
INSERT INTO "Events" ("Id", "Title", "Description", "ImageUrl", "EventType", "Status", "StartDate", "EndDate", "Location", "VirtualMeetingUrl", "Capacity", "WaitlistCapacity", "AvailableSpots", "IsPrivate", "RequiresVetting", "MinimumAge", "SkillLevel", "DressCode", "EquipmentProvided", "EquipmentToBring", "Prerequisites", "CancellationPolicy", "Tags", "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt", "CancelledAt", "CancellationReason", "VenueName", "VenueAddress", "VenueCity", "VenueState", "VenueZipCode", "VenueCountry", "VenueAccessibilityInfo", "ParkingInfo", "PublicTransportInfo", "CheckInTime", "DoorsCloseTime", "LastEntryTime", "FeaturedUntil", "PricingTiers", "EarlyBirdEndDate", "MemberDiscount", "MultiEventDiscount", "IsRecurring", "RecurrencePattern", "RecurrenceEndDate", "ParentEventId")
VALUES
(gen_random_uuid(), 'Monthly Rope Social', 'Casual social gathering for all skill levels. Practice, learn from others, or just hang out with the rope community. Light refreshments provided.', NULL, 3, 1, NOW() + INTERVAL '25 days' + INTERVAL '18 hours', NOW() + INTERVAL '25 days' + INTERVAL '21 hours', 'The Rope Space - All Rooms', NULL, 80, 0, 80, false, false, 18, 0, 'Casual', false, 'Optional: your own rope', NULL, 'No cancellation needed', '["social","all-levels","free","community"]', '$ADMIN_ID'::uuid, NOW(), NOW(), NOW(), NULL, NULL, 'The Rope Space', '123 Main St', 'Salem', 'MA', '01970', 'USA', 'Wheelchair accessible', 'Street parking available', 'Bus stop 2 blocks away', NOW() + INTERVAL '25 days' + INTERVAL '17 hours' + INTERVAL '45 minutes', NULL, NULL, NULL, '[{"amount":0.00,"currency":"USD"}]'::jsonb, NULL, 0.0, NULL, true, 'Monthly', NOW() + INTERVAL '365 days', NULL);
EOF

echo "Database seeded successfully!"
echo ""
echo "Test accounts created:"
echo "  Admin: admin@witchcityrope.com / Test123!"
echo "  Teacher: teacher@witchcityrope.com / Test123!" 
echo "  Vetted Member: vetted@witchcityrope.com / Test123!"
echo "  General Member: member@witchcityrope.com / Test123!"