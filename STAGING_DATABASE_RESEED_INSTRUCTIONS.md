# Staging Database Reseed Instructions - October 24, 2025

## Overview
This document provides instructions for reseeding the staging database on DigitalOcean to apply the HTML-formatted event seed data.

## What Changed
All 8 events in the seed data now have **HTML-formatted descriptions and policies** with proper tags:
- `<h2>`, `<h3>` headings
- `<ul>` and `<li>` for bullet lists
- `<p>` for paragraphs
- `<strong>` and `<em>` for emphasis

## Prerequisites
- SSH access to DigitalOcean droplet
- DigitalOcean CLI (`doctl`) installed and authenticated
- Access to staging database credentials

## Option 1: Deploy Latest Code (Recommended)
This will deploy the updated SeedDataService.cs with HTML-formatted content.

### Steps:
1. **Commit and push the HTML seed data changes:**
   ```bash
   git add apps/api/Services/SeedDataService.cs
   git commit -m "feat(seed): convert event descriptions and policies to HTML format

   - Added HTML tags (<h2>, <h3>, <ul>, <li>, <p>, <strong>, <em>) to all 8 events
   - Event descriptions now render properly with formatted content
   - Event policies use semantic HTML for better accessibility
   - Local database verified with HTML content

   All events affected:
   - Introduction to Rope Safety
   - Suspension Basics
   - Advanced Floor Work
   - Community Rope Jam
   - Rope Social & Discussion
   - New Members Meetup
   - Beginner Rope Circle (past)
   - Rope Fundamentals Series (past)"

   git push origin main
   ```

2. **Trigger DigitalOcean App Platform deployment:**
   - Go to https://cloud.digitalocean.com/apps
   - Navigate to your WitchCityRope app
   - Click "Actions" → "Force Rebuild and Deploy"
   - Wait for deployment to complete (~5-10 minutes)

3. **Clear events from staging database:**
   SSH into your droplet or use DigitalOcean console:

   ```bash
   # Connect to staging database (get credentials from App Platform environment variables)
   psql "postgresql://DB_USER:DB_PASSWORD@DB_HOST:DB_PORT/DB_NAME"

   # Clear events and related data
   DELETE FROM "EventParticipations";
   DELETE FROM "Sessions";
   DELETE FROM "TicketTypes";
   DELETE FROM "VolunteerPositions";
   DELETE FROM "Events";

   # Verify deletion
   SELECT COUNT(*) FROM "Events";  -- Should return 0

   \q
   ```

4. **Restart the API service to trigger reseeding:**
   - In DigitalOcean App Platform, go to your app
   - Click on the "api" component
   - Click "Actions" → "Restart"
   - Monitor logs for "Starting sample event creation" message

5. **Verify HTML content in staging database:**
   ```bash
   psql "postgresql://DB_USER:DB_PASSWORD@DB_HOST:DB_PORT/DB_NAME"

   SELECT "Title", LEFT("Description", 100) as description_preview
   FROM "Events"
   WHERE "Title" = 'Introduction to Rope Safety';

   -- Should show: <p>This comprehensive introduction to rope safety...

   SELECT "Title", LEFT("Policies", 150) as policies_preview
   FROM "Events"
   WHERE "Title" = 'Introduction to Rope Safety';

   -- Should show: <h2>Event Policies and Safety Guidelines</h2>...
   ```

## Option 2: Manual Database Script (Alternative)
If you can't deploy new code immediately, you can manually update the database.

**Note:** This is more complex and error-prone. Option 1 is strongly recommended.

### Steps:
1. Connect to staging database
2. For each of the 8 events, update the Description and Policies fields with HTML content
3. This requires carefully escaping quotes and ensuring proper SQL syntax

**Not recommended** - Use Option 1 instead.

## Verification Checklist
After reseeding, verify:

- [ ] Events table has 8 records
- [ ] All event descriptions contain HTML tags (`<p>`, `<h3>`, `<ul>`, `<li>`)
- [ ] All event policies contain HTML tags (`<h2>`, `<h3>`, `<ul>`, `<li>`)
- [ ] Frontend correctly renders HTML (check event detail pages)
- [ ] No rendering errors in browser console
- [ ] Policy sections display with proper headings and bullet points

## Testing Frontend Rendering
1. Visit staging URL: https://your-staging-url.ondigitalocean.app
2. Navigate to Events page
3. Click on "Introduction to Rope Safety" event
4. Verify:
   - Description shows formatted content with headings and bullets
   - Policies section shows formatted guidelines with headings and bullets
   - No raw HTML tags visible (e.g., no `<p>` text showing)
   - Proper semantic structure (headings, lists, paragraphs)

## Troubleshooting

### Issue: Seed data not populating
**Solution:**
- Ensure ALL events were deleted (check EventParticipations, Sessions, TicketTypes, VolunteerPositions first)
- Restart API service to trigger seeding
- Check API logs for "Seed data already exists, skipping population" - if you see this, you didn't delete all records

### Issue: HTML tags showing as plain text
**Solution:**
- Check frontend rendering component
- Ensure you're using `dangerouslySetInnerHTML` or proper HTML rendering
- Verify Content Security Policy allows inline HTML

### Issue: Deployment fails
**Solution:**
- Check DigitalOcean build logs
- Verify C# code compiles locally first
- Ensure all string literals are properly escaped

## Rollback Plan
If HTML formatting causes issues:

1. **Revert code changes:**
   ```bash
   git revert <commit-hash>
   git push origin main
   ```

2. **Trigger redeployment** (Option 1 steps 2-4)

3. **Or manually update database** to remove HTML tags (complex, not recommended)

## Notes
- Local database already verified with HTML content
- Staging reseed should match local exactly
- Always test in staging before production
- Keep database backups before major changes

## Database Connection String Format
```
postgresql://USERNAME:PASSWORD@HOST:PORT/DATABASE
```

Get credentials from DigitalOcean App Platform:
- Go to your app → Settings → Environment Variables (Runtime)
- Look for DATABASE_URL or individual DB_* variables

---

**Created:** October 24, 2025
**Related Commit:** (pending - create after following Option 1, Step 1)
**Local Database:** ✅ Verified with HTML content
**Staging Database:** ⏳ Pending reseed
