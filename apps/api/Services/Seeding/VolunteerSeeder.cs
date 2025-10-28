using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
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
            else
            {
                // Add event-wide volunteer positions for other events
                var eventPositions = CreateEventVolunteerPositions(eventItem);
                volunteerPositionsToAdd.AddRange(eventPositions);

                // Add session-specific volunteer positions for multi-day events
                if (eventItem.Sessions.Any() && eventItem.EventType == EventType.Class)
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

        // Common volunteer positions for all events - associated with main session
        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            SessionId = mainSession.Id,
            Title = "Door Monitor",
            Description = "Check attendees in, verify tickets/RSVPs, and welcome newcomers",
            SlotsNeeded = 2,
            SlotsFilled = 0, // Will be set by actual signups
            IsPublicFacing = true // Public can sign up
        });

        positions.Add(new VolunteerPosition
        {
            EventId = eventItem.Id,
            SessionId = mainSession.Id,
            Title = "Setup/Cleanup Crew",
            Description = "Help set up equipment before the event and clean up afterwards",
            SlotsNeeded = 3,
            SlotsFilled = 0, // Will be set by actual signups
            IsPublicFacing = true // Public can sign up
        });

        // Additional positions for classes
        if (eventItem.EventType == EventType.Class)
        {
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = mainSession.Id,
                Title = "Teaching Assistant",
                Description = "Help instructor with demonstrations and assist students",
                SlotsNeeded = 1,
                SlotsFilled = 0, // Will be set by actual signups
                IsPublicFacing = false // Admin-only assignment
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
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = $"Session Monitor - {session.Name}",
                Description = $"Monitor safety and assist during {session.Name}",
                SlotsNeeded = 1,
                SlotsFilled = 0, // Will be set by actual signups
                IsPublicFacing = false // Admin-only assignment (requires safety expertise)
            });
        }

        return positions;
    }

    /// <summary>
    /// Creates session-specific volunteer positions for Suspension Basics event.
    /// This demonstrates volunteer positions tied to specific sessions for testing.
    /// Different positions for Day 1 (Setup Crew, Door Monitor) and Day 2 (Teaching Assistant, Safety Monitor).
    /// </summary>
    private List<VolunteerPosition> CreateSuspensionBasicsVolunteerPositions(Event eventItem, Session session)
    {
        var positions = new List<VolunteerPosition>();

        if (session.SessionCode == "DAY1")
        {
            // Day 1 positions (6:00 PM - 9:00 PM)
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Setup Crew",
                Description = "Help set up equipment and prepare the space before Day 1 begins",
                SlotsNeeded = 3,
                SlotsFilled = 0,
                IsPublicFacing = true
            });

            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Door Monitor",
                Description = "Check attendees in and welcome participants for Day 1",
                SlotsNeeded = 2,
                SlotsFilled = 0,
                IsPublicFacing = true
            });
        }
        else if (session.SessionCode == "DAY2")
        {
            // Day 2 positions (8:00 PM - 10:00 PM)
            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Teaching Assistant",
                Description = "Assist instructor with advanced suspension demonstrations and provide individual feedback for Day 2",
                SlotsNeeded = 2,
                SlotsFilled = 0,
                IsPublicFacing = true
            });

            positions.Add(new VolunteerPosition
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Title = "Safety Monitor",
                Description = "Monitor rigging and ensure safety protocols are followed during Day 2",
                SlotsNeeded = 2,
                SlotsFilled = 0,
                IsPublicFacing = true
            });
        }

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

        // Check if historical volunteer positions already exist
        var historicalPositionsExist = await _context.VolunteerPositions
            .AnyAsync(vp => vp.EventId == eventSeeder.AdvancedSuspensionEventId, cancellationToken);

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
                EventId = eventSeeder.AdvancedSuspensionEventId,
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
                EventId = eventSeeder.RopeFundamentalsEventId,
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
                EventId = eventSeeder.PracticeNightEventId,
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
                EventId = eventSeeder.WelcomeMixerEventId,
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
