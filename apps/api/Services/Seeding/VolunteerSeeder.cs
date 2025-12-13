using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of volunteer positions and signup assignments.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating VolunteerPosition records for events/sessions and VolunteerSignup records.
/// </summary>
public class VolunteerSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EventSeeder _eventSeeder;
    private readonly ILogger<VolunteerSeeder> _logger;
    private static readonly TimeZoneInfo _easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public VolunteerSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        EventSeeder eventSeeder,
        ILogger<VolunteerSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _eventSeeder = eventSeeder;
        _logger = logger;
    }

    /// <summary>
    /// Seeds volunteer positions for all events and their sessions.
    /// Idempotent operation - skips if volunteer positions already exist.
    ///
    /// Creates different position types based on event:
    /// - Suspension Basics: Session-specific volunteer positions (Day 1, Day 2)
    /// - Multi-day events (Classes): Event-wide + session-specific positions
    /// - Single-day events: Event-wide positions only
    ///
    /// Common positions: Door Monitor, Setup/Cleanup Crew
    /// Class-specific positions: Teaching Assistant, Session Monitor
    ///
    /// After creating positions, automatically creates volunteer signups for testing.
    /// </summary>
    public async Task SeedVolunteerPositionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting volunteer positions creation");

        // Check if volunteer positions already exist (idempotent operation)
        var existingVolunteerCount = await _context.VolunteerPositions.CountAsync(cancellationToken);
        if (existingVolunteerCount > 0)
        {
            _logger.LogInformation("Volunteer positions already exist ({Count}), skipping volunteer seeding", existingVolunteerCount);
            return;
        }

        var now = DateTime.UtcNow;
        var events = await _context.Events
            .Include(e => e.Sessions)
            .Where(e => e.EndDate >= now)
            .ToListAsync(cancellationToken);

        var volunteerPositionsToAdd = new List<VolunteerPosition>();

        foreach (var eventItem in events)
        {
            // Special handling for Suspension Basics: Create session-specific volunteer positions
            if (eventItem.Title.Contains("Suspension Basics"))
            {
                foreach (var session in eventItem.Sessions)
                {
                    var sessionPositions = CreateSuspensionBasicsVolunteerPositions(eventItem, session);
                    volunteerPositionsToAdd.AddRange(sessionPositions);
                }
            }
            // Special handling for comprehensive timing test events: Create S1-specific, S2-specific, and event-wide positions
            else if (eventItem.Title.StartsWith("Timing Test - "))
            {
                var timingPositions = CreateTimingTestVolunteerPositions(eventItem);
                volunteerPositionsToAdd.AddRange(timingPositions);
            }
            else
            {
                // Add event-wide volunteer positions for other events
                var eventPositions = CreateEventVolunteerPositions(eventItem);
                volunteerPositionsToAdd.AddRange(eventPositions);

                // Add session-specific volunteer positions for multi-day events
                if (eventItem.Sessions.Any() && eventItem.RequireTicketPurchase)
                {
                    foreach (var session in eventItem.Sessions)
                    {
                        var sessionPositions = CreateSessionVolunteerPositions(eventItem, session);
                        volunteerPositionsToAdd.AddRange(sessionPositions);
                    }
                }
            }
        }

        await _context.VolunteerPositions.AddRangeAsync(volunteerPositionsToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Volunteer positions creation completed. Created: {VolunteerCount} positions", volunteerPositionsToAdd.Count);

        // Create volunteer signups for testing
        await SeedVolunteerSignupsAsync(cancellationToken);
    }

    /// <summary>
    /// Creates volunteer signups for testing volunteer management functionality.
    /// Idempotent operation - skips if signups already exist.
    ///
    /// Creates signups with varied statuses:
    /// - Multiple confirmed signups for upcoming events (admin, teacher, vetted, member)
    /// - One completed signup for past event (with check-in and completion timestamps)
    ///
    /// Updates position SlotsFilled counts to match signups.
    /// </summary>
    private async Task SeedVolunteerSignupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting volunteer signups creation");

        // Check if volunteer signups already exist (idempotent operation)
        var existingSignupCount = await _context.VolunteerSignups.CountAsync(cancellationToken);
        if (existingSignupCount > 0)
        {
            _logger.LogInformation("Volunteer signups already exist ({Count}), skipping signup seeding", existingSignupCount);
            return;
        }

        // Get users for signup testing
        var adminUser = await _userManager.FindByEmailAsync("admin@witchcityrope.com");
        var teacherUser = await _userManager.FindByEmailAsync("teacher@witchcityrope.com");
        var vettedUser = await _userManager.FindByEmailAsync("vetted@witchcityrope.com");
        var memberUser = await _userManager.FindByEmailAsync("member@witchcityrope.com");

        if (adminUser == null || teacherUser == null || vettedUser == null || memberUser == null)
        {
            _logger.LogWarning("Could not find all test users for volunteer signup seeding");
            return;
        }

        // Get volunteer positions
        var volunteerPositions = await _context.VolunteerPositions
            .Include(vp => vp.Event)
            .ToListAsync(cancellationToken);

        if (!volunteerPositions.Any())
        {
            _logger.LogWarning("No volunteer positions found for signup seeding");
            return;
        }

        var signupsToAdd = new List<VolunteerSignup>();
        var now = DateTime.UtcNow;

        // RopeMaster (admin) volunteers for multiple positions
        // Sign up for Door Monitor positions (public-facing)
        var doorMonitorPositions = volunteerPositions
            .Where(vp => vp.Title == "Door Monitor" && vp.IsPublicFacing)
            .Take(3)
            .ToList();

        foreach (var position in doorMonitorPositions)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = position.Id,
                UserId = adminUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-7),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7)
            });

            // Update position slot count
            position.SlotsFilled++;
        }

        // RopeMaster volunteers for Setup/Cleanup
        var setupPosition = volunteerPositions
            .Where(vp => vp.Title == "Setup/Cleanup Crew" && vp.IsPublicFacing)
            .FirstOrDefault();

        if (setupPosition != null)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = setupPosition.Id,
                UserId = adminUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-5),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            });

            setupPosition.SlotsFilled++;
        }

        // Teacher volunteers for Setup/Cleanup
        var setupPosition2 = volunteerPositions
            .Where(vp => vp.Title == "Setup/Cleanup Crew" && vp.IsPublicFacing && vp.Id != setupPosition?.Id)
            .FirstOrDefault();

        if (setupPosition2 != null)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = setupPosition2.Id,
                UserId = teacherUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-6),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-6)
            });

            setupPosition2.SlotsFilled++;
        }

        // Vetted member volunteers for Door Monitor
        var doorPosition = volunteerPositions
            .Where(vp => vp.Title == "Door Monitor" && vp.IsPublicFacing && !doorMonitorPositions.Contains(vp))
            .FirstOrDefault();

        if (doorPosition != null)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = doorPosition.Id,
                UserId = vettedUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-4),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-4)
            });

            doorPosition.SlotsFilled++;
        }

        // Regular member volunteers for Setup/Cleanup
        var setupPosition3 = volunteerPositions
            .Where(vp => vp.Title == "Setup/Cleanup Crew" && vp.IsPublicFacing &&
                         vp.Id != setupPosition?.Id && vp.Id != setupPosition2?.Id)
            .FirstOrDefault();

        if (setupPosition3 != null)
        {
            signupsToAdd.Add(new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = setupPosition3.Id,
                UserId = memberUser.Id,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = now.AddDays(-3),
                HasCheckedIn = false,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-3)
            });

            setupPosition3.SlotsFilled++;
        }

        // Add one completed volunteer signup for RopeMaster (past event)
        var pastPosition = volunteerPositions
            .Where(vp => vp.Event != null && vp.Event.StartDate < now && vp.IsPublicFacing)
            .FirstOrDefault();

        if (pastPosition != null)
        {
            var completedSignup = new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = pastPosition.Id,
                UserId = adminUser.Id,
                Status = VolunteerSignupStatus.Completed,
                SignedUpAt = now.AddDays(-14),
                HasCheckedIn = true,
                CheckedInAt = pastPosition.Event!.StartDate.AddMinutes(-15),
                HasCompleted = true,
                CompletedAt = pastPosition.Event!.StartDate.AddHours(2),
                CreatedAt = now.AddDays(-14),
                UpdatedAt = pastPosition.Event!.StartDate.AddHours(2)
            };

            signupsToAdd.Add(completedSignup);
            pastPosition.SlotsFilled++;
        }

        await _context.VolunteerSignups.AddRangeAsync(signupsToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Volunteer signups creation completed. Created: {SignupCount} signups", signupsToAdd.Count);
    }

    /// <summary>
    /// Creates event-wide volunteer positions.
    /// Common positions for all events: Door Monitor, Setup/Cleanup Crew.
    /// Additional positions for classes: Teaching Assistant.
    /// </summary>
    private List<VolunteerPosition> CreateEventVolunteerPositions(Event eventItem)
    {
        var positions = new List<VolunteerPosition>();

        // Get the main session for this event (all events have at least one session)
        var mainSession = eventItem.Sessions.FirstOrDefault();
        if (mainSession == null)
        {
            _logger.LogWarning("Event {EventId} has no sessions, cannot create volunteer positions", eventItem.Id);
            return positions;
        }

        // Calculate times based on session
        var sessionStartLocal = TimeZoneInfo.ConvertTimeFromUtc(mainSession.StartTime, _easternTimeZone);
        var sessionEndLocal = TimeZoneInfo.ConvertTimeFromUtc(mainSession.EndTime, _easternTimeZone);
        var sessionStartTimeStr = sessionStartLocal.ToString("HH:mm");
        var sessionEndTimeStr = sessionEndLocal.ToString("HH:mm");
        var setupStartTimeStr = sessionStartLocal.AddMinutes(-30).ToString("HH:mm");
        var cleanupEndTimeStr = sessionEndLocal.AddMinutes(30).ToString("HH:mm");

        // Common volunteer positions for all events - associated with main session
        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            SessionId = mainSession.Id,
            Title = "Door Monitor",
            Description = "Check attendees in, verify tickets/RSVPs, and welcome newcomers",
            SlotsNeeded = 2,
            SlotsFilled = 0, // Will be set by actual signups
            IsPublicFacing = true, // Public can sign up
            StartTime = sessionStartTimeStr,
            EndTime = sessionEndTimeStr
        });

        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            SessionId = mainSession.Id,
            Title = "Setup/Cleanup Crew",
            Description = "Help set up equipment before the event and clean up afterwards",
            SlotsNeeded = 3,
            SlotsFilled = 0, // Will be set by actual signups
            IsPublicFacing = true, // Public can sign up
            StartTime = setupStartTimeStr,
            EndTime = cleanupEndTimeStr
        });

        // Additional positions for classes
        if (eventItem.RequireTicketPurchase)
        {
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = mainSession.Id,
                Title = "Teaching Assistant",
                Description = "Help instructor with demonstrations and assist students",
                SlotsNeeded = 1,
                SlotsFilled = 0, // Will be set by actual signups
                IsPublicFacing = false, // Admin-only assignment
                StartTime = sessionStartTimeStr,
                EndTime = sessionEndTimeStr
            });
        }

        return positions;
    }

    /// <summary>
    /// Creates session-specific volunteer positions for multi-day events.
    /// Only creates positions for sessions beyond the first one.
    /// </summary>
    private List<VolunteerPosition> CreateSessionVolunteerPositions(Event eventItem, Session session)
    {
        var positions = new List<VolunteerPosition>();

        // Session-specific positions only for multi-day events
        if (session.SessionCode != "S1" || session.Name.Contains("Day"))
        {
            // Calculate times based on session
            var sessionStartLocal = TimeZoneInfo.ConvertTimeFromUtc(session.StartTime, _easternTimeZone);
            var sessionEndLocal = TimeZoneInfo.ConvertTimeFromUtc(session.EndTime, _easternTimeZone);
            var sessionStartTimeStr = sessionStartLocal.ToString("HH:mm");
            var sessionEndTimeStr = sessionEndLocal.ToString("HH:mm");

            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = $"Session Monitor - {session.Name}",
                Description = $"Monitor safety and assist during {session.Name}",
                SlotsNeeded = 1,
                SlotsFilled = 0, // Will be set by actual signups
                IsPublicFacing = false, // Admin-only assignment (requires safety expertise)
                StartTime = sessionStartTimeStr,
                EndTime = sessionEndTimeStr
            });
        }

        return positions;
    }

    /// <summary>
    /// Creates session-specific volunteer positions for Suspension Basics event.
    /// This demonstrates volunteer positions tied to specific sessions for testing.
    /// Both Day 1 and Day 2 have the same positions: Check-in Monitor (1 slot) and Event Setup (2 slots).
    /// </summary>
    private List<VolunteerPosition> CreateSuspensionBasicsVolunteerPositions(Event eventItem, Session session)
    {
        var positions = new List<VolunteerPosition>();

        // Both Day 1 and Day 2 get the same volunteer positions
        if (session.SessionCode == "DAY1" || session.SessionCode == "DAY2")
        {
            var dayLabel = session.SessionCode == "DAY1" ? "Day One" : "Day Two";

            // Get session times for volunteer shift times
            var sessionStartLocal = TimeZoneInfo.ConvertTimeFromUtc(session.StartTime, _easternTimeZone);
            var sessionEndLocal = TimeZoneInfo.ConvertTimeFromUtc(session.EndTime, _easternTimeZone);
            var sessionStartTimeStr = sessionStartLocal.ToString("HH:mm");
            var sessionEndTimeStr = sessionEndLocal.ToString("HH:mm");
            // Setup starts 30 minutes before session
            var setupStartTimeStr = sessionStartLocal.AddMinutes(-30).ToString("HH:mm");

            // Check-in Monitor: 1 slot per session
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Check-in Monitor",
                Description = $"Check attendees in, verify tickets, and welcome participants for {dayLabel}",
                SlotsNeeded = 1,
                SlotsFilled = 0,
                IsPublicFacing = true,
                StartTime = sessionStartTimeStr,
                EndTime = sessionEndTimeStr
            });

            // Event Setup: 2 slots per session
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Event Setup",
                Description = $"Help set up equipment and prepare the space before {dayLabel} begins",
                SlotsNeeded = 2,
                SlotsFilled = 0,
                IsPublicFacing = true,
                StartTime = setupStartTimeStr,
                EndTime = sessionStartTimeStr
            });
        }

        return positions;
    }

    // ====================================================================================
    // COMPREHENSIVE TIMING TEST VOLUNTEER POSITIONS
    // ====================================================================================
    // Creates volunteer positions for testing session-based volunteer timing.
    // For each timing test event (6hr, 48hr, 300hr close), creates:
    // - S1-specific position: Uses S1's timing (24hr future)
    // - S2-specific position: Uses S2's timing (120hr future)
    // - Event-wide position (no SessionId): Uses EARLIEST session timing (S1 @ 24hr)
    //
    // BUSINESS RULES TESTED:
    // - Session-specific positions use their session's StartTime
    // - Event-wide positions use EARLIEST session's StartTime
    // - Window is OPEN if: hoursUntilSession >= volunteerRegistrationCloseHours
    // ====================================================================================

    /// <summary>
    /// Creates volunteer positions for comprehensive timing test events.
    /// Creates exactly 3 positions per event to test all volunteer timing scenarios:
    /// - S1-specific: Uses S1's StartTime (24hr future)
    /// - S2-specific: Uses S2's StartTime (120hr future)
    /// - Event-wide (no SessionId): Uses EARLIEST session's StartTime (S1 @ 24hr)
    /// </summary>
    private List<VolunteerPosition> CreateTimingTestVolunteerPositions(Event eventItem)
    {
        var positions = new List<VolunteerPosition>();

        var s1Session = eventItem.Sessions.FirstOrDefault(s => s.SessionCode == "S1");
        var s2Session = eventItem.Sessions.FirstOrDefault(s => s.SessionCode == "S2");

        if (s1Session == null || s2Session == null)
        {
            _logger.LogWarning("Timing test event {Title} missing sessions, cannot create volunteer positions", eventItem.Title);
            return positions;
        }

        var closeHours = eventItem.VolunteerRegistrationCloseHours ?? 0;

        // Calculate times for each session
        var s1StartLocal = TimeZoneInfo.ConvertTimeFromUtc(s1Session.StartTime, _easternTimeZone);
        var s1EndLocal = TimeZoneInfo.ConvertTimeFromUtc(s1Session.EndTime, _easternTimeZone);
        var s2StartLocal = TimeZoneInfo.ConvertTimeFromUtc(s2Session.StartTime, _easternTimeZone);
        var s2EndLocal = TimeZoneInfo.ConvertTimeFromUtc(s2Session.EndTime, _easternTimeZone);
        var s1StartTimeStr = s1StartLocal.ToString("HH:mm");
        var s1EndTimeStr = s1EndLocal.ToString("HH:mm");
        var s2StartTimeStr = s2StartLocal.ToString("HH:mm");
        var s2EndTimeStr = s2EndLocal.ToString("HH:mm");
        var s1SetupStartStr = s1StartLocal.AddMinutes(-30).ToString("HH:mm");

        // Position 1: S1-specific (uses S1 @ 24hr for timing)
        // Available if: 24 >= volunteerRegistrationCloseHours
        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            SessionId = s1Session.Id,
            Title = "S1 Setup Helper",
            Description = $"Help with Session 1 setup (24hr future). Close window: {closeHours}hr. Expected: {(24 >= (double)closeHours ? "OPEN" : "CLOSED")}",
            SlotsNeeded = 2,
            SlotsFilled = 0,
            IsPublicFacing = true,
            StartTime = s1SetupStartStr,
            EndTime = s1StartTimeStr
        });

        // Position 2: S2-specific (uses S2 @ 120hr for timing)
        // Available if: 120 >= volunteerRegistrationCloseHours
        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            SessionId = s2Session.Id,
            Title = "S2 Safety Monitor",
            Description = $"Monitor safety during Session 2 (120hr future). Close window: {closeHours}hr. Expected: {(120 >= (double)closeHours ? "OPEN" : "CLOSED")}",
            SlotsNeeded = 2,
            SlotsFilled = 0,
            IsPublicFacing = true,
            StartTime = s2StartTimeStr,
            EndTime = s2EndTimeStr
        });

        // Position 3: Event-wide (no SessionId, uses EARLIEST session = S1 @ 24hr)
        // Available if: 24 >= volunteerRegistrationCloseHours (uses S1's timing)
        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            SessionId = null, // Event-wide = uses EARLIEST session for timing
            Title = "Event Coordinator",
            Description = $"Coordinate across both sessions. Uses EARLIEST session (S1 @ 24hr) for timing. Close window: {closeHours}hr. Expected: {(24 >= (double)closeHours ? "OPEN" : "CLOSED")}",
            SlotsNeeded = 1,
            SlotsFilled = 0,
            IsPublicFacing = true,
            StartTime = s1StartTimeStr,
            EndTime = s2EndTimeStr  // Coordinator covers both sessions
        });

        _logger.LogInformation(
            "Created volunteer positions for {Title}: S1 Helper ({S1Status}), S2 Monitor ({S2Status}), Event Coordinator ({EventStatus})",
            eventItem.Title,
            24 >= (double)closeHours ? "OPEN" : "CLOSED",
            120 >= (double)closeHours ? "OPEN" : "CLOSED",
            24 >= (double)closeHours ? "OPEN" : "CLOSED");

        return positions;
    }

    /// <summary>
    /// Seeds volunteer positions and signups for the 4 historical events.
    /// These represent past workshops and social events with completed volunteer assignments.
    /// Idempotent operation - skips if historical positions already exist.
    /// Called explicitly by SeedCoordinator to ensure proper ordering of seed operations.
    /// </summary>
    public async Task SeedHistoricalVolunteerPositionsAsync(EventSeeder eventSeeder, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting historical volunteer positions creation");

        // Load historical event IDs from database (EventSeeder might have skipped seeding if events already exist)
        var advancedSuspensionEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Title == "Advanced Suspension Techniques", cancellationToken);
        var ropeFundamentalsEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Title == "Rope Fundamentals Intensive", cancellationToken);
        var practiceNightEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Title == "Monthly Practice Night", cancellationToken);
        var welcomeMixerEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Title == "New Members Welcome Mixer", cancellationToken);

        // If events don't exist, skip volunteer position seeding
        if (advancedSuspensionEvent == null || ropeFundamentalsEvent == null ||
            practiceNightEvent == null || welcomeMixerEvent == null)
        {
            _logger.LogWarning("Historical events not found in database, skipping volunteer positions");
            return;
        }

        // Check if historical volunteer positions already exist
        var historicalPositionsExist = await _context.VolunteerPositions
            .AnyAsync(vp => vp.EventId == advancedSuspensionEvent.Id, cancellationToken);

        if (historicalPositionsExist)
        {
            _logger.LogInformation("Historical volunteer positions already exist, skipping");
            return;
        }

        // Define position-volunteer mappings for all 4 historical events
        var volunteerData = new[]
        {
            // Workshop 1: Advanced Suspension Techniques (75 days ago)
            new
            {
                EventId = advancedSuspensionEvent.Id,
                DaysAgo = 77,
                Positions = new[]
                {
                    new
                    {
                        Title = "Setup Crew",
                        Description = "Help set up suspension equipment and safety mats before the workshop",
                        Slots = 2,
                        RequiresVetting = false,
                        Volunteers = new[] { "member@witchcityrope.com", "vetted@witchcityrope.com" }
                    },
                    new
                    {
                        Title = "Safety Monitor",
                        Description = "Monitor participant safety during suspension sessions",
                        Slots = 2,
                        RequiresVetting = true,
                        Volunteers = new[] { "coordinator1@witchcityrope.com", "coordinator2@witchcityrope.com" }
                    }
                }
            },

            // Workshop 2: Rope Fundamentals Intensive (60 days ago)
            new
            {
                EventId = ropeFundamentalsEvent.Id,
                DaysAgo = 62,
                Positions = new[]
                {
                    new
                    {
                        Title = "Registration Desk",
                        Description = "Check in participants and distribute materials at registration desk",
                        Slots = 2,
                        RequiresVetting = false,
                        Volunteers = new[] { "member@witchcityrope.com", "guest@witchcityrope.com" }
                    },
                    new
                    {
                        Title = "Photography",
                        Description = "Take photos during the workshop for community archive (faces optional)",
                        Slots = 1,
                        RequiresVetting = false,
                        Volunteers = new[] { "teacher@witchcityrope.com" }
                    }
                }
            },

            // Social Event 1: Monthly Rope Practice Night (45 days ago)
            new
            {
                EventId = practiceNightEvent.Id,
                DaysAgo = 47,
                Positions = new[]
                {
                    new
                    {
                        Title = "Space Setup",
                        Description = "Set up practice space with mats and organize rope storage",
                        Slots = 2,
                        RequiresVetting = false,
                        Volunteers = new[] { "vetted@witchcityrope.com", "member@witchcityrope.com" }
                    },
                    new
                    {
                        Title = "Cleanup Crew",
                        Description = "Help clean up and pack away equipment at end of practice night",
                        Slots = 2,
                        RequiresVetting = false,
                        Volunteers = new[] { "coordinator1@witchcityrope.com", "guest@witchcityrope.com" }
                    }
                }
            },

            // Social Event 2: New Member Welcome Mixer (30 days ago)
            new
            {
                EventId = welcomeMixerEvent.Id,
                DaysAgo = 32,
                Positions = new[]
                {
                    new
                    {
                        Title = "Greeter",
                        Description = "Welcome new members and help them feel comfortable",
                        Slots = 2,
                        RequiresVetting = false,
                        Volunteers = new[] { "teacher@witchcityrope.com", "coordinator2@witchcityrope.com" }
                    },
                    new
                    {
                        Title = "Info Table",
                        Description = "Staff the information table and answer questions about the community",
                        Slots = 1,
                        RequiresVetting = false,
                        Volunteers = new[] { "admin@witchcityrope.com" }
                    }
                }
            }
        };

        var positionsCreated = 0;
        var assignmentsCreated = 0;

        // Create positions and assignments for each event
        foreach (var eventData in volunteerData)
        {
            foreach (var positionData in eventData.Positions)
            {
                var position = new VolunteerPosition
                {
                    Id = Guid.NewGuid(),
                    EventId = eventData.EventId,
                    Title = positionData.Title,
                    Description = positionData.Description,
                    SlotsNeeded = positionData.Slots,
                    SlotsFilled = positionData.Volunteers.Length, // Set to actual volunteer count
                    IsPublicFacing = !positionData.RequiresVetting,
                    CreatedAt = DateTime.UtcNow.AddDays(-eventData.DaysAgo),
                    UpdatedAt = DateTime.UtcNow.AddDays(-eventData.DaysAgo)
                };
                _context.VolunteerPositions.Add(position);
                positionsCreated++;

                // Create volunteer signups for each volunteer
                foreach (var volunteerEmail in positionData.Volunteers)
                {
                    var volunteer = await _userManager.FindByEmailAsync(volunteerEmail);
                    if (volunteer != null)
                    {
                        var signup = new VolunteerSignup
                        {
                            Id = Guid.NewGuid(),
                            VolunteerPositionId = position.Id,
                            UserId = volunteer.Id,
                            Status = VolunteerSignupStatus.Confirmed,
                            SignedUpAt = position.CreatedAt.AddDays(1), // Assigned 1 day after position created
                            HasCheckedIn = false,
                            CreatedAt = position.CreatedAt.AddDays(1),
                            UpdatedAt = position.CreatedAt.AddDays(1)
                        };
                        _context.VolunteerSignups.Add(signup);
                        assignmentsCreated++;
                    }
                    else
                    {
                        _logger.LogWarning("Volunteer user not found: {Email}", volunteerEmail);
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Historical volunteer positions creation completed. Created: {PositionCount} positions, {AssignmentCount} signups",
            positionsCreated, assignmentsCreated);
    }
}
