-- PostgreSQL Database Seeding Script for WitchCityRope
-- This script seeds test data matching the actual schema from the migration

-- Clear existing data (in reverse order of dependencies)
DELETE FROM "RefreshTokens";
DELETE FROM "IncidentActions";
DELETE FROM "IncidentReviews";
DELETE FROM "IncidentReports";
DELETE FROM "Payments";
DELETE FROM "Registrations";
DELETE FROM "EventOrganizers";
DELETE FROM "VettingApplications";
DELETE FROM "UserAuthentications";
DELETE FROM "Events";
DELETE FROM "Users";

-- Seed Users
-- Note: EncryptedLegalName would normally be encrypted, but for seeding we'll use placeholder encrypted values
-- The password for all test users is: Test123! (BCrypt hash: $2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu)

-- Admin User
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "IsActive", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "IsVetted")
VALUES 
('a1111111-1111-1111-1111-111111111111', 'encrypted_admin_legal_name', 'Rope Master Admin', 'admin@witchcityrope.com', '1990-01-01'::timestamp, 'Admin', true, NOW(), NOW(), 'Rope Master Admin', 'they/them', true);

-- Teacher User
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "IsActive", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "IsVetted")
VALUES 
('b2222222-2222-2222-2222-222222222222', 'encrypted_teacher_legal_name', 'Mistress Knots', 'teacher@witchcityrope.com', '1985-05-15'::timestamp, 'Teacher', true, NOW(), NOW(), 'Mistress Knots', 'she/her', true);

-- Vetted Member
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "IsActive", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "IsVetted")
VALUES 
('c3333333-3333-3333-3333-333333333333', 'encrypted_vetted_legal_name', 'Salem Bound', 'vetted@witchcityrope.com', '1992-08-20'::timestamp, 'VettedMember', true, NOW(), NOW(), 'Salem Bound', 'he/him', true);

-- General Member
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "IsActive", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "IsVetted")
VALUES 
('d4444444-4444-4444-4444-444444444444', 'encrypted_member_legal_name', 'Rope Curious', 'member@witchcityrope.com', '1995-12-10'::timestamp, 'Member', true, NOW(), NOW(), 'Rope Curious', 'she/they', false);

-- Additional Members for variety
INSERT INTO "Users" ("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "IsActive", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "IsVetted")
VALUES 
('e5555555-5555-5555-5555-555555555555', 'encrypted_alice_legal_name', 'Witch City Alice', 'alice@example.com', '1988-03-25'::timestamp, 'VettedMember', true, NOW() - INTERVAL '30 days', NOW(), 'Witch City Alice', 'she/her', true),
('f6666666-6666-6666-6666-666666666666', 'encrypted_bob_legal_name', 'Bondage Bob', 'bob@example.com', '1993-07-14'::timestamp, 'Member', true, NOW() - INTERVAL '15 days', NOW(), 'Bondage Bob', 'he/him', false),
('a7777777-7777-7777-7777-777777777777', 'encrypted_charlie_legal_name', 'Charlie Chains', 'charlie@example.com', '1991-11-30'::timestamp, 'Attendee', true, NOW() - INTERVAL '5 days', NOW(), 'Charlie Chains', 'they/them', false);

-- Seed UserAuthentications (passwords for test accounts)
-- Password for all: Test123! (BCrypt hash)
INSERT INTO "UserAuthentications" ("Id", "UserId", "PasswordHash", "TwoFactorSecret", "IsTwoFactorEnabled", "LastPasswordChangeAt", "FailedLoginAttempts", "LockedOutUntil", "CreatedAt", "UpdatedAt")
VALUES
('aa111111-1111-1111-1111-111111111111', 'a1111111-1111-1111-1111-111111111111', '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', NULL, false, NOW(), 0, NULL, NOW(), NOW()),
('bb222222-2222-2222-2222-222222222222', 'b2222222-2222-2222-2222-222222222222', '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', NULL, false, NOW(), 0, NULL, NOW(), NOW()),
('cc333333-3333-3333-3333-333333333333', 'c3333333-3333-3333-3333-333333333333', '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', NULL, false, NOW(), 0, NULL, NOW(), NOW()),
('dd444444-4444-4444-4444-444444444444', 'd4444444-4444-4444-4444-444444444444', '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', NULL, false, NOW(), 0, NULL, NOW(), NOW()),
('ee555555-5555-5555-5555-555555555555', 'e5555555-5555-5555-5555-555555555555', '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', NULL, false, NOW(), 0, NULL, NOW(), NOW()),
('ff666666-6666-6666-6666-666666666666', 'f6666666-6666-6666-6666-666666666666', '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', NULL, false, NOW(), 0, NULL, NOW(), NOW()),
('aa777777-7777-7777-7777-777777777777', 'a7777777-7777-7777-7777-777777777777', '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', NULL, false, NOW(), 0, NULL, NOW(), NOW());

-- Seed Events
-- Workshop Event (upcoming)
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers")
VALUES
('e1111111-1111-1111-1111-111111111111', 'Introduction to Rope Bondage', 'Learn the basics of rope bondage in a safe, inclusive environment. This workshop covers fundamental ties, safety protocols, and consent practices.', 
NOW() + INTERVAL '7 days', NOW() + INTERVAL '7 days' + INTERVAL '3 hours', 20, 'Workshop', 'WitchCity Community Center, Salem MA', true, NOW() - INTERVAL '14 days', NOW(),
'[{"Amount": 50.00, "Currency": "USD"}, {"Amount": 75.00, "Currency": "USD"}, {"Amount": 100.00, "Currency": "USD"}]');

-- Performance Event (upcoming)
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers")
VALUES
('e2222222-2222-2222-2222-222222222222', 'Midnight Rope Performance', 'An artistic rope performance showcasing the beauty and artistry of rope bondage. Featured performers from around New England.', 
NOW() + INTERVAL '14 days', NOW() + INTERVAL '14 days' + INTERVAL '2 hours', 100, 'Performance', 'Salem Arts Theatre', true, NOW() - INTERVAL '21 days', NOW(),
'[{"Amount": 25.00, "Currency": "USD"}, {"Amount": 35.00, "Currency": "USD"}, {"Amount": 50.00, "Currency": "USD"}]');

-- Social Event (upcoming)
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers")
VALUES
('e3333333-3333-3333-3333-333333333333', 'Monthly Rope Social', 'Our regular monthly social gathering for rope enthusiasts. Practice your skills, meet new people, and enjoy our community.', 
NOW() + INTERVAL '21 days', NOW() + INTERVAL '21 days' + INTERVAL '4 hours', 50, 'Social', 'WitchCity Dungeon Space', true, NOW() - INTERVAL '7 days', NOW(),
'[{"Amount": 15.00, "Currency": "USD"}, {"Amount": 20.00, "Currency": "USD"}, {"Amount": 30.00, "Currency": "USD"}]');

-- Advanced Workshop (upcoming)
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers")
VALUES
('e4444444-4444-4444-4444-444444444444', 'Advanced Suspension Techniques', 'For experienced riggers only. Learn advanced suspension techniques with a focus on safety and dynamic movement.', 
NOW() + INTERVAL '30 days', NOW() + INTERVAL '30 days' + INTERVAL '4 hours', 12, 'Workshop', 'Private Studio, Salem MA', true, NOW() - INTERVAL '5 days', NOW(),
'[{"Amount": 100.00, "Currency": "USD"}, {"Amount": 125.00, "Currency": "USD"}, {"Amount": 150.00, "Currency": "USD"}]');

-- Past Event (for history)
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers")
VALUES
('e5555555-5555-5555-5555-555555555555', 'Halloween Rope Party', 'Special Halloween-themed rope party with costumes encouraged!', 
NOW() - INTERVAL '60 days', NOW() - INTERVAL '60 days' + INTERVAL '5 hours', 75, 'Social', 'WitchCity Dungeon Space', true, NOW() - INTERVAL '90 days', NOW() - INTERVAL '60 days',
'[{"Amount": 20.00, "Currency": "USD"}, {"Amount": 30.00, "Currency": "USD"}, {"Amount": 40.00, "Currency": "USD"}]');

-- Seed EventOrganizers (link events to organizers)
INSERT INTO "EventOrganizers" ("EventId", "UserId")
VALUES
('e1111111-1111-1111-1111-111111111111', 'b2222222-2222-2222-2222-222222222222'), -- Teacher organizing intro workshop
('e2222222-2222-2222-2222-222222222222', 'a1111111-1111-1111-1111-111111111111'), -- Admin organizing performance
('e2222222-2222-2222-2222-222222222222', 'b2222222-2222-2222-2222-222222222222'), -- Teacher also organizing performance
('e3333333-3333-3333-3333-333333333333', 'c3333333-3333-3333-3333-333333333333'), -- Vetted member organizing social
('e4444444-4444-4444-4444-444444444444', 'b2222222-2222-2222-2222-222222222222'), -- Teacher organizing advanced workshop
('e5555555-5555-5555-5555-555555555555', 'a1111111-1111-1111-1111-111111111111'); -- Admin organized past event

-- Seed VettingApplications
INSERT INTO "VettingApplications" ("Id", "ApplicantId", "ExperienceLevel", "Interests", "SafetyKnowledge", "ExperienceDescription", 
"ConsentUnderstanding", "WhyJoin", "Status", "SubmittedAt", "UpdatedAt", "ReviewedAt", "DecisionNotes", "References")
VALUES
-- Pending application from general member
('v1111111-1111-1111-1111-111111111111', 'd4444444-4444-4444-4444-444444444444', 
'Beginner - I have attended a few workshops', 
'I am interested in learning both topping and bottoming, with a focus on floor work initially.',
'I understand the importance of checking circulation, nerve function, and having safety scissors nearby.',
'I have attended 3 workshops at other venues and have been practicing basic column ties and chest harnesses.',
'Consent is ongoing and can be revoked at any time. Clear communication and regular check-ins are essential.',
'I want to join to be part of a supportive community where I can learn and grow in my rope journey safely.',
'Pending', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NULL, '',
'[{"Name": "Rope Mentor Mike", "Contact": "mike@example.com"}, {"Name": "Workshop Leader Lisa", "Contact": "lisa@example.com"}]'),

-- Another pending application
('v2222222-2222-2222-2222-222222222222', 'f6666666-6666-6666-6666-666666666666',
'Intermediate - I have been practicing for 2 years',
'Primarily interested in artistic rope and photography. Some interest in rope suspension.',
'I know about nerve pathways, proper placement to avoid injury, and emergency release techniques.',
'Two years of regular practice, including several intensives. Comfortable with TKs and hip harnesses.',
'Enthusiastic consent is key. I believe in negotiation before play and continuous communication during.',
'Looking for a community that values safety and artistic expression. Want to contribute my photography skills.',
'Pending', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NULL, '',
'[{"Name": "Previous Partner Pat", "Contact": "pat@example.com"}]');

-- Display summary
SELECT 'Database seeded successfully!' as message;
SELECT 'Users created:' as section, COUNT(*) as count FROM "Users"
UNION ALL
SELECT 'Events created:', COUNT(*) FROM "Events"
UNION ALL
SELECT 'User Authentications created:', COUNT(*) FROM "UserAuthentications"
UNION ALL
SELECT 'Event Organizers created:', COUNT(*) FROM "EventOrganizers"
UNION ALL
SELECT 'Vetting Applications created:', COUNT(*) FROM "VettingApplications";