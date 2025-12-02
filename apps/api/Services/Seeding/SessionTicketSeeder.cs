using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Models;
using WitchCityRope.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of event sessions and ticket types.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating sessions for events and their associated ticket type configurations.
/// </summary>
public class SessionTicketSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionTicketSeeder> _logger;

    public SessionTicketSeeder(
        ApplicationDbContext context,
        ILogger<SessionTicketSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds sessions and ticket types for all events.
    /// Idempotent operation - skips if sessions already exist.
    ///
    /// Creates sessions based on event type:
    /// - Single-day events: One session
    /// - Suspension Basics: Multiple sessions (Day 1, Day 2) for testing session-specific features
    /// - Multi-day events (Intensives, Conferences): Multiple day sessions
    ///
    /// Creates ticket types based on event type:
    /// - Social events: Free RSVP + optional sliding scale donation
    /// - Class events: Regular ticket pricing
    /// - Multi-day events: Individual day tickets + full event ticket with discount
    ///
    /// Note: Sessions MUST be saved before ticket types are created (ticket types reference session IDs).
    /// </summary>
    public async Task SeedSessionsAndTicketsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sessions and ticket types creation");

        // Check if sessions already exist (idempotent operation)
        var existingSessionCount = await _context.Sessions.CountAsync(cancellationToken);
        if (existingSessionCount > 0)
        {
            _logger.LogInformation("Sessions already exist ({Count}), skipping session and ticket seeding", existingSessionCount);
            return;
        }

        var events = await _context.Events.ToListAsync(cancellationToken);
        var sessionsToAdd = new List<Session>();

        // First, create sessions without ticket types
        foreach (var eventItem in events)
        {
            if (eventItem.Title.Contains("Suspension Intensive") || eventItem.Title.Contains("Conference"))
            {
                // Multi-day events (2-3 days)
                var numberOfDays = eventItem.Title.Contains("Conference") ? 3 : 2;
                AddMultiDayEventSessions(eventItem, numberOfDays, sessionsToAdd);
            }
            else if (eventItem.Title.Contains("Suspension Basics"))
            {
                // Multi-session event (for testing session-specific volunteer positions)
                AddSuspensionBasicsSessions(eventItem, sessionsToAdd);
            }
            else if (eventItem.Title == "Advanced Suspension Techniques")
            {
                // Historical Event 1: Two sessions (Morning and Afternoon)
                AddAdvancedSuspensionSessions(eventItem, sessionsToAdd);
            }
            else if (eventItem.Title == "Rope Fundamentals Intensive")
            {
                // Historical Event 2: Three sessions (Basics, Core Ties, Practice)
                AddRopeFundamentalsIntensiveSessions(eventItem, sessionsToAdd);
            }
            else if (eventItem.Title == "Session Timing Test Event")
            {
                // E2E Test Event: S1 in past, S2 in future (for testing session-based timing)
                AddSessionTimingTestEventSessions(eventItem, sessionsToAdd);
            }
            else if (eventItem.Title == "Timing Test - 1hr Until Close")
            {
                // Edge case event: Single session at 25 hours (window closes in ~1 hour with 24hr close)
                AddEdgeCaseTimingTestSession(eventItem, sessionsToAdd);
            }
            else if (eventItem.Title.StartsWith("Timing Test - "))
            {
                // Comprehensive timing test events: S1=24hrs, S2=120hrs (for testing all timing scenarios)
                AddComprehensiveTimingTestSessions(eventItem, sessionsToAdd);
            }
            else
            {
                // Single-day events (most events)
                AddSingleDayEventSession(eventItem, sessionsToAdd);
            }
        }

        // Save sessions first to get their IDs
        await _context.Sessions.AddRangeAsync(sessionsToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Now create ticket types with valid session IDs
        var ticketTypesToAdd = new List<TicketType>();

        // Group sessions by event to handle multi-day events
        var sessionsByEvent = sessionsToAdd.GroupBy(s => s.EventId).ToList();

        foreach (var eventGroup in sessionsByEvent)
        {
            var eventItem = events.First(e => e.Id == eventGroup.Key);
            var eventSessions = eventGroup.ToList();

            // Special handling for historical events with custom ticket structures
            if (eventItem.Title == "Advanced Suspension Techniques")
            {
                CreateAdvancedSuspensionTicketTypes(eventItem, eventSessions, ticketTypesToAdd);
            }
            else if (eventItem.Title == "Rope Fundamentals Intensive")
            {
                CreateRopeFundamentalsIntensiveTicketTypes(eventItem, eventSessions, ticketTypesToAdd);
            }
            else if (eventItem.Title == "Session Timing Test Event")
            {
                // E2E Test Event: Create specific ticket types for testing session-based timing
                CreateSessionTimingTestEventTicketTypes(eventItem, eventSessions, ticketTypesToAdd);
            }
            else if (eventItem.Title == "Timing Test - 1hr Until Close")
            {
                // Edge case event: Single ticket for the 25-hour session
                CreateEdgeCaseTimingTestTicketTypes(eventItem, eventSessions, ticketTypesToAdd);
            }
            else if (eventItem.Title.StartsWith("Timing Test - "))
            {
                // Comprehensive timing test events: Create S1 Only, S2 Only, Both Sessions tickets
                CreateComprehensiveTimingTestTicketTypes(eventItem, eventSessions, ticketTypesToAdd);
            }
            else if (eventItem.Title == "Monthly Rope Practice Night" || eventItem.Title == "New Member Welcome Mixer")
            {
                // Social events with single session and sliding scale donation
                CreateSocialEventTicketTypes(eventItem, eventSessions.First(), ticketTypesToAdd);
            }
            else if (eventSessions.Count > 1)
            {
                // Multi-day event - create both individual day tickets and full event tickets
                var basePrice = eventItem.EventType == EventType.Social ? 0m : 40m; // Multi-day events default to $40
                CreateMultiDayTicketTypes(eventItem, basePrice, eventSessions, ticketTypesToAdd);
            }
            else
            {
                // Single-day event
                var price = eventItem.EventType == EventType.Social ? 10m : 25m; // Default pricing for seed data
                CreateTicketTypesForSession(eventItem, price, eventSessions.First(), ticketTypesToAdd);
            }
        }

        await _context.TicketTypes.AddRangeAsync(ticketTypesToAdd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sessions and ticket types creation completed. Created: {SessionCount} sessions, {TicketCount} ticket types",
            sessionsToAdd.Count, ticketTypesToAdd.Count);
    }

    /// <summary>
    /// Helper method to add a single session for single-day events.
    /// Creates a "Main Session" that spans the full event duration.
    /// </summary>
    private void AddSingleDayEventSession(Event eventItem, List<Session> sessionsToAdd)
    {
        var session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "S1",
            Name = "Main Session",
            StartTime = eventItem.StartDate,
            EndTime = eventItem.EndDate,
            Capacity = eventItem.Capacity
        };

        sessionsToAdd.Add(session);
    }

    /// <summary>
    /// Helper method to add sessions for Suspension Basics event (multi-session example).
    /// Creates two sessions: Day 1 and Day 2 for testing session-specific volunteers.
    /// </summary>
    private void AddSuspensionBasicsSessions(Event eventItem, List<Session> sessionsToAdd)
    {
        // Day 1: 6:00 PM - 9:00 PM
        var day1Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "DAY1",
            Name = "Day 1",
            StartTime = eventItem.StartDate,
            EndTime = eventItem.StartDate.AddHours(3),
            Capacity = eventItem.Capacity / 2
        };

        // Day 2: 8:00 PM - 10:00 PM
        var day2Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "DAY2",
            Name = "Day 2",
            StartTime = eventItem.StartDate.AddHours(2),
            EndTime = eventItem.StartDate.AddHours(4),
            Capacity = eventItem.Capacity / 2
        };

        sessionsToAdd.Add(day1Session);
        sessionsToAdd.Add(day2Session);
    }

    /// <summary>
    /// Helper method to add sessions for multi-day events.
    /// Creates multiple day sessions (ticket types added separately).
    /// </summary>
    private void AddMultiDayEventSessions(Event eventItem, int numberOfDays, List<Session> sessionsToAdd)
    {
        for (int day = 1; day <= numberOfDays; day++)
        {
            var daySession = new Session
            {
                EventId = eventItem.Id,
                SessionCode = $"D{day}",
                Name = $"Day {day}",
                StartTime = eventItem.StartDate.AddDays(day - 1),
                EndTime = eventItem.StartDate.AddDays(day - 1).AddHours(8), // 8 hour sessions
                Capacity = (int)Math.Ceiling(eventItem.Capacity / (double)numberOfDays)
            };

            sessionsToAdd.Add(daySession);
        }
    }

    /// <summary>
    /// Helper method to create ticket types for a session.
    /// Must be called after session has been saved and has a valid ID.
    ///
    /// For social events: Creates free RSVP + optional sliding scale donation.
    /// For class events: Creates regular ticket only.
    /// </summary>
    private void CreateTicketTypesForSession(Event eventItem, decimal price, Session session, List<TicketType> ticketTypesToAdd)
    {
        if (eventItem.EventType == EventType.Social)
        {
            // Social events: Free RSVP + optional sliding scale donation ticket
            var rsvpTicket = new TicketType
            {
                EventId = eventItem.Id,
                Sessions = new List<Session> { session },
                Name = "Free RSVP",
                Description = "Free attendance - RSVP required",
                Price = 0,
                Available = session.Capacity,
                PricingType = PricingType.Fixed
            };

            var donationTicket = new TicketType
            {
                EventId = eventItem.Id,
                Sessions = new List<Session> { session },
                Name = "Support Donation",
                Description = "Optional sliding scale donation to support the community - pay what you can!",
                Price = null, // Not used for sliding scale
                MinPrice = 10m,
                MaxPrice = 40m,
                DefaultPrice = 20m,
                Available = session.Capacity,
                PricingType = PricingType.SlidingScale
            };

            ticketTypesToAdd.Add(rsvpTicket);
            ticketTypesToAdd.Add(donationTicket);
        }
        else // Class
        {
            // Class events: Regular ticket only
            var regularTicket = new TicketType
            {
                EventId = eventItem.Id,
                Sessions = new List<Session> { session },
                Name = "Regular",
                Description = "Full access to the workshop",
                Price = price,
                Available = session.Capacity,
                PricingType = PricingType.Fixed
            };

            ticketTypesToAdd.Add(regularTicket);
        }
    }

    /// <summary>
    /// Helper method to create ticket types for multi-day events.
    /// Creates individual day tickets and full event ticket with discount.
    /// For social events, uses sliding scale pricing.
    /// </summary>
    private void CreateMultiDayTicketTypes(Event eventItem, decimal basePrice, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        if (eventItem.EventType == EventType.Social)
        {
            // Social events: Free RSVP for each day + sliding scale donation for full event
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var rsvpTicket = new TicketType
                {
                    EventId = eventItem.Id,
                    Sessions = new List<Session> { session },
                    Name = $"Day {i + 1} RSVP",
                    Description = $"Free RSVP for Day {i + 1} only",
                    Price = 0,
                    Available = session.Capacity,
                    PricingType = PricingType.Fixed
                };

                ticketTypesToAdd.Add(rsvpTicket);
            }

            // Full event sliding scale donation (includes all sessions)
            var fullEventTicket = new TicketType
            {
                EventId = eventItem.Id,
                Sessions = sessions.ToList(), // Multi-session ticket includes all sessions
                Name = $"All {sessions.Count} Days Support",
                Description = $"Optional sliding scale donation for all {sessions.Count} days - pay what you can!",
                Price = null, // Not used for sliding scale
                MinPrice = 10m,
                MaxPrice = 40m,
                DefaultPrice = 20m,
                Available = eventItem.Capacity,
                PricingType = PricingType.SlidingScale
            };

            ticketTypesToAdd.Add(fullEventTicket);
        }
        else // Class
        {
            var dailyPrice = Math.Round(basePrice * 0.6m, 2); // Individual day is 60% of full price

            // Create individual day tickets
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var dayTicket = new TicketType
                {
                    EventId = eventItem.Id,
                    Sessions = new List<Session> { session },
                    Name = $"Day {i + 1} Only",
                    Description = $"Access to Day {i + 1} activities only",
                    Price = dailyPrice,
                    Available = session.Capacity,
                    PricingType = PricingType.Fixed
                };

                ticketTypesToAdd.Add(dayTicket);
            }

            // Create full event ticket with discount (includes all sessions)
            var fullEventTicket = new TicketType
            {
                EventId = eventItem.Id,
                Sessions = sessions.ToList(), // Multi-session ticket includes all sessions
                Name = $"All {sessions.Count} Days",
                Description = $"Full access to all {sessions.Count} days - SAVE ${(dailyPrice * sessions.Count - basePrice):F0}!",
                Price = basePrice,
                Available = eventItem.Capacity,
                PricingType = PricingType.Fixed
            };

            ticketTypesToAdd.Add(fullEventTicket);
        }
    }

    /// <summary>
    /// Helper method to add sessions for Advanced Suspension Techniques historical event.
    /// Creates Morning Session (Foundation) and Afternoon Session (Advanced).
    /// </summary>
    private void AddAdvancedSuspensionSessions(Event eventItem, List<Session> sessionsToAdd)
    {
        // Session 1: Morning Session - Foundation (9 AM - 12 PM)
        var morningSession = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "MORNING",
            Name = "Morning Session - Foundation",
            StartTime = eventItem.StartDate,
            EndTime = eventItem.StartDate.AddHours(3),
            Capacity = eventItem.Capacity
        };

        // Session 2: Afternoon Session - Advanced (1 PM - 4 PM)
        var afternoonSession = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "AFTERNOON",
            Name = "Afternoon Session - Advanced",
            StartTime = eventItem.StartDate.AddHours(4), // 1 PM
            EndTime = eventItem.StartDate.AddHours(7),   // 4 PM
            Capacity = eventItem.Capacity
        };

        sessionsToAdd.Add(morningSession);
        sessionsToAdd.Add(afternoonSession);
    }

    /// <summary>
    /// Helper method to add sessions for Rope Fundamentals Intensive historical event.
    /// Creates three sessions covering Basics, Core Ties, and Practice.
    /// </summary>
    private void AddRopeFundamentalsIntensiveSessions(Event eventItem, List<Session> sessionsToAdd)
    {
        // Session 1: Basics and Safety (9 AM - 11:30 AM)
        var basicsSession = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "BASICS",
            Name = "Basics and Safety",
            StartTime = eventItem.StartDate,
            EndTime = eventItem.StartDate.AddHours(2.5),
            Capacity = eventItem.Capacity
        };

        // Session 2: Core Ties (12 PM - 2:30 PM)
        var coreTiesSession = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "CORE",
            Name = "Core Ties",
            StartTime = eventItem.StartDate.AddHours(3),
            EndTime = eventItem.StartDate.AddHours(5.5),
            Capacity = eventItem.Capacity
        };

        // Session 3: Practice and Review (3 PM - 5:30 PM)
        var practiceSession = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "PRACTICE",
            Name = "Practice and Review",
            StartTime = eventItem.StartDate.AddHours(6),
            EndTime = eventItem.StartDate.AddHours(8.5),
            Capacity = eventItem.Capacity
        };

        sessionsToAdd.Add(basicsSession);
        sessionsToAdd.Add(coreTiesSession);
        sessionsToAdd.Add(practiceSession);
    }

    /// <summary>
    /// Creates ticket types for Advanced Suspension Techniques historical event.
    /// Full Workshop Pass ($80), Single Session Ticket ($45), Early Bird Full Pass ($65).
    /// </summary>
    private void CreateAdvancedSuspensionTicketTypes(Event eventItem, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        // Full Workshop Pass - $80 (access to both sessions)
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = sessions.ToList(), // Multi-session ticket includes all sessions
            Name = "Full Workshop Pass",
            Description = "Access to both morning and afternoon sessions",
            Price = 80.00m,
            Available = 15,
            PricingType = PricingType.Fixed
        });

        // Single Session Ticket - $45 (can be used for either session)
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = sessions.ToList(), // Can be used for either session
            Name = "Single Session Ticket",
            Description = "Access to one session (morning or afternoon)",
            Price = 45.00m,
            Available = 10,
            PricingType = PricingType.Fixed
        });

        // Early Bird Full Pass - $65
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = sessions.ToList(), // Multi-session ticket includes all sessions
            Name = "Early Bird Full Pass",
            Description = "Discounted full workshop access",
            Price = 65.00m,
            Available = 5,
            PricingType = PricingType.Fixed
        });
    }

    /// <summary>
    /// Creates ticket types for Rope Fundamentals Intensive historical event.
    /// Full Day Pass ($100), Half Day Pass ($60), Single Session ($35).
    /// </summary>
    private void CreateRopeFundamentalsIntensiveTicketTypes(Event eventItem, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        // Full Day Pass - $100 (all sessions)
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = sessions.ToList(), // Multi-session ticket includes all sessions
            Name = "Full Day Pass",
            Description = "Access to all three sessions",
            Price = 100.00m,
            Available = 20,
            PricingType = PricingType.Fixed
        });

        // Half Day Pass - $60 (any 2 sessions)
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = sessions.ToList(), // Flexible ticket, includes all sessions
            Name = "Half Day Pass",
            Description = "Access to any two sessions",
            Price = 60.00m,
            Available = 10,
            PricingType = PricingType.Fixed
        });

        // Single Session - $35
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = sessions.ToList(), // Flexible ticket, includes all sessions
            Name = "Single Session",
            Description = "Access to one session",
            Price = 35.00m,
            Available = 5,
            PricingType = PricingType.Fixed
        });
    }

    /// <summary>
    /// Creates ticket types for social historical events (Practice Night, Welcome Mixer).
    /// Suggested Donation with sliding scale pricing.
    /// </summary>
    private void CreateSocialEventTicketTypes(Event eventItem, Session session, List<TicketType> ticketTypesToAdd)
    {
        // Suggested Donation - sliding scale with minimum of $5
        var suggestedPrice = eventItem.Title.Contains("Practice Night") ? 10.00m : 5.00m;

        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = new List<Session> { session },
            Name = "Suggested Donation",
            Description = eventItem.Title.Contains("Practice Night")
                ? "Optional donation to support the space"
                : "Optional donation to support refreshments",
            Price = suggestedPrice,
            MinPrice = 5.00m, // FIXED: Minimum $5 donation for all social event tickets
            MaxPrice = suggestedPrice * 4,
            DefaultPrice = suggestedPrice,
            Available = eventItem.Capacity,
            PricingType = PricingType.SlidingScale
        });
    }

    /// <summary>
    /// Helper method to add sessions for Session Timing Test Event (E2E test event).
    /// Creates two sessions on DIFFERENT DATES:
    /// - S1: 7 days in the PAST (already passed)
    /// - S2: 5 days in the FUTURE (upcoming)
    ///
    /// This enables testing the session-based timing logic where:
    /// - S1-only tickets should NOT be available (session passed)
    /// - S2-only tickets SHOULD be available (future session)
    /// - Both sessions ticket uses S2 as reference session
    /// </summary>
    private void AddSessionTimingTestEventSessions(Event eventItem, List<Session> sessionsToAdd)
    {
        // S1: Past session (7 days ago at 6 PM)
        // eventItem.StartDate is already set to 7 days ago
        var s1Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "S1",
            Name = "Session 1 - Past",
            StartTime = eventItem.StartDate, // 7 days ago
            EndTime = eventItem.StartDate.AddHours(3),
            Capacity = eventItem.Capacity / 2
        };

        // S2: Future session (5 days from now at 6 PM)
        // Calculate: Event started 7 days ago, S2 is 12 days after event start = 5 days from now
        var s2StartTime = DateTime.UtcNow.AddDays(5).Date.AddHours(18); // 5 days from now, 6 PM
        var s2Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "S2",
            Name = "Session 2 - Future",
            StartTime = DateTime.SpecifyKind(s2StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(s2StartTime.AddHours(3), DateTimeKind.Utc),
            Capacity = eventItem.Capacity / 2
        };

        sessionsToAdd.Add(s1Session);
        sessionsToAdd.Add(s2Session);
    }

    /// <summary>
    /// Creates ticket types for Session Timing Test Event (E2E test event).
    /// Creates three ticket types to test session-based timing:
    /// - S1 Only: Links to S1 session only (should NOT be available - session passed)
    /// - S2 Only: Links to S2 session only (SHOULD be available - future session)
    /// - Both Sessions: Multi-session ticket (uses S2 as reference per spec)
    ///
    /// Expected behavior when E2E test runs:
    /// - S1 Only: referenceSessionId = null, canPurchase = false
    /// - S2 Only: referenceSessionId = S2.Id, canPurchase = true
    /// - Both Sessions: referenceSessionId = S2.Id (first future), check spec for canPurchase
    /// </summary>
    private void CreateSessionTimingTestEventTicketTypes(Event eventItem, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        var s1Session = sessions.FirstOrDefault(s => s.SessionCode == "S1");
        var s2Session = sessions.FirstOrDefault(s => s.SessionCode == "S2");

        // S1 Only Ticket - Links to past session only
        // Expected: NOT available (all sessions for this ticket have passed)
        if (s1Session != null)
        {
            ticketTypesToAdd.Add(new TicketType
            {
                EventId = eventItem.Id,
                Sessions = new List<Session> { s1Session }, // Single session = S1 only
                Name = "S1 Only Ticket",
                Description = "Access to Session 1 only (past session - for testing)",
                Price = 25.00m,
                Available = 10,
                PricingType = PricingType.Fixed
            });
        }

        // S2 Only Ticket - Links to future session only
        // Expected: Available (session is 5 days away, > 12 hour close window)
        if (s2Session != null)
        {
            ticketTypesToAdd.Add(new TicketType
            {
                EventId = eventItem.Id,
                Sessions = new List<Session> { s2Session }, // Single session = S2 only
                Name = "S2 Only Ticket",
                Description = "Access to Session 2 only (future session - for testing)",
                Price = 25.00m,
                Available = 10,
                PricingType = PricingType.Fixed
            });
        }

        // Both Sessions Ticket - Multi-session ticket
        // Expected: Uses S2 as reference session (first future session per spec)
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = sessions.ToList(), // Multi-session ticket includes all sessions
            Name = "Both Sessions Ticket",
            Description = "Access to both Session 1 and Session 2 (for testing multi-session timing)",
            Price = 40.00m,
            Available = 10,
            PricingType = PricingType.Fixed
        });
    }

    // ====================================================================================
    // COMPREHENSIVE TIMING TEST METHODS
    // ====================================================================================
    // These methods create sessions and ticket types for testing ALL timing scenarios.
    // Used by "Timing Test - 6hr Close", "Timing Test - 48hr Close", "Timing Test - 300hr Close" events.
    //
    // SESSION CONFIGURATION:
    // - S1: 24 hours (1 day) in the future
    // - S2: 120 hours (5 days) in the future
    //
    // TICKET TYPES:
    // - S1 Only: Single-session ticket for S1
    // - S2 Only: Single-session ticket for S2
    // - Both Sessions: Multi-session ticket (SessionId = null, uses EARLIEST session for timing)
    //
    // BUSINESS RULES BEING TESTED:
    // 1. Single-session tickets use their specific session's StartTime
    // 2. Multi-session tickets use EARLIEST session's StartTime (not first-future)
    // 3. Timing window is OPEN if: hoursUntilSession >= closeHours
    // 4. Timing window is CLOSED if: hoursUntilSession < closeHours
    // ====================================================================================

    /// <summary>
    /// Adds sessions for comprehensive timing test events.
    /// Creates exactly 2 sessions with FIXED future times:
    /// - S1: 24 hours (1 day) in the future
    /// - S2: 120 hours (5 days) in the future
    ///
    /// These specific times are chosen to create interesting test scenarios:
    /// - With 6hr close: Both sessions open (24 > 6, 120 > 6)
    /// - With 48hr close: S1 closed, S2 open (24 < 48, 120 > 48)
    /// - With 300hr close: Both sessions closed (24 < 300, 120 < 300)
    /// </summary>
    private void AddComprehensiveTimingTestSessions(Event eventItem, List<Session> sessionsToAdd)
    {
        // S1: Exactly 24 hours (1 day) in the future at 6 PM
        var s1StartTime = DateTime.UtcNow.AddHours(24).Date.AddHours(18);
        // Adjust to exactly 24 hours from now (not date boundary)
        s1StartTime = DateTime.UtcNow.AddHours(24);

        var s1Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "S1",
            Name = "Session 1 - 24hr Future",
            StartTime = DateTime.SpecifyKind(s1StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(s1StartTime.AddHours(3), DateTimeKind.Utc),
            Capacity = eventItem.Capacity / 2
        };

        // S2: Exactly 120 hours (5 days) in the future
        var s2StartTime = DateTime.UtcNow.AddHours(120);

        var s2Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "S2",
            Name = "Session 2 - 120hr Future",
            StartTime = DateTime.SpecifyKind(s2StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(s2StartTime.AddHours(3), DateTimeKind.Utc),
            Capacity = eventItem.Capacity / 2
        };

        sessionsToAdd.Add(s1Session);
        sessionsToAdd.Add(s2Session);

        _logger.LogInformation(
            "Created comprehensive timing test sessions for {EventTitle}: S1 @ {S1Time} UTC (in {S1Hours}hrs), S2 @ {S2Time} UTC (in {S2Hours}hrs)",
            eventItem.Title,
            s1StartTime,
            (s1StartTime - DateTime.UtcNow).TotalHours,
            s2StartTime,
            (s2StartTime - DateTime.UtcNow).TotalHours);
    }

    /// <summary>
    /// Creates ticket types for comprehensive timing test events.
    /// Creates exactly 3 ticket types:
    /// - S1 Only: Single-session ticket linked to S1 (uses S1's timing)
    /// - S2 Only: Single-session ticket linked to S2 (uses S2's timing)
    /// - Both Sessions: Multi-session ticket (SessionId = null, uses EARLIEST session = S1)
    ///
    /// Expected behavior based on Event.RegistrationCloseHours:
    /// ====================================================================================
    /// | Event          | CloseHrs | S1 (24hr) | S2 (120hr) | Both (uses S1) |
    /// |----------------|----------|-----------|------------|----------------|
    /// | 6hr Close      |     6    | OPEN      | OPEN       | OPEN           |
    /// | 48hr Close     |    48    | CLOSED    | OPEN       | CLOSED         |
    /// | 300hr Close    |   300    | CLOSED    | CLOSED     | CLOSED         |
    /// ====================================================================================
    /// </summary>
    private void CreateComprehensiveTimingTestTicketTypes(Event eventItem, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        var s1Session = sessions.FirstOrDefault(s => s.SessionCode == "S1");
        var s2Session = sessions.FirstOrDefault(s => s.SessionCode == "S2");

        if (s1Session == null || s2Session == null)
        {
            _logger.LogWarning("Missing sessions for timing test event {Title}", eventItem.Title);
            return;
        }

        // Extract close hours from event for documentation in ticket description
        var closeHours = eventItem.RegistrationCloseHours ?? 0;

        // S1 Only Ticket - Links to session 24 hours in the future
        // Available if: 24 >= closeHours
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = new List<Session> { s1Session },
            Name = "S1 Only Ticket",
            Description = $"Access to Session 1 only (24hr future). Close window: {closeHours}hr. Expected: {(24 >= (double)closeHours ? "OPEN" : "CLOSED")}",
            Price = 25.00m,
            Available = 10,
            PricingType = PricingType.Fixed
        });

        // S2 Only Ticket - Links to session 120 hours in the future
        // Available if: 120 >= closeHours
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = new List<Session> { s2Session },
            Name = "S2 Only Ticket",
            Description = $"Access to Session 2 only (120hr future). Close window: {closeHours}hr. Expected: {(120 >= (double)closeHours ? "OPEN" : "CLOSED")}",
            Price = 25.00m,
            Available = 10,
            PricingType = PricingType.Fixed
        });

        // Both Sessions Ticket - Multi-session (uses EARLIEST session = S1 for timing)
        // Available if: 24 >= closeHours (because multi-session uses S1's timing)
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = new List<Session> { s1Session, s2Session }, // Multi-session includes both
            Name = "Both Sessions Ticket",
            Description = $"Access to both sessions. Uses EARLIEST session (S1 @ 24hr) for timing. Close window: {closeHours}hr. Expected: {(24 >= (double)closeHours ? "OPEN" : "CLOSED")}",
            Price = 40.00m,
            Available = 10,
            PricingType = PricingType.Fixed
        });

        _logger.LogInformation(
            "Created ticket types for {Title}: S1 Only ({S1Status}), S2 Only ({S2Status}), Both ({BothStatus})",
            eventItem.Title,
            24 >= (double)closeHours ? "OPEN" : "CLOSED",
            120 >= (double)closeHours ? "OPEN" : "CLOSED",
            24 >= (double)closeHours ? "OPEN" : "CLOSED");
    }

    // ====================================================================================
    // EDGE CASE TIMING TEST (1 Hour Until Close)
    // ====================================================================================
    // Tests UTC storage vs local time display at the edge case where window closes in ~1 hour.
    //
    // CONFIGURATION:
    // - Single session at 25 hours from now
    // - RegistrationCloseHours = 24 (set on event)
    // - Window closes in: 25 - 24 = 1 hour
    //
    // PURPOSE:
    // Verifies timezone conversions work correctly when registration is about to close.
    // ====================================================================================

    /// <summary>
    /// Adds a single session for the edge case timing test event.
    /// Session is EXACTLY 25 hours from seed time, so with 24-hour close window,
    /// registration closes in ~1 hour.
    /// </summary>
    private void AddEdgeCaseTimingTestSession(Event eventItem, List<Session> sessionsToAdd)
    {
        // Session at exactly 25 hours from now
        // With RegistrationCloseHours = 24, window closes in 1 hour (25 - 24 = 1)
        var sessionStartTime = DateTime.UtcNow.AddHours(25);

        var session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "EDGE",
            Name = "Edge Case Session - 25hr Future",
            StartTime = DateTime.SpecifyKind(sessionStartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(sessionStartTime.AddHours(3), DateTimeKind.Utc),
            Capacity = eventItem.Capacity
        };

        sessionsToAdd.Add(session);

        var hoursUntilClose = 25 - (double)(eventItem.RegistrationCloseHours ?? 24);
        _logger.LogInformation(
            "Created edge case timing test session for {EventTitle}: Session @ {SessionTime} UTC (in 25hr), close window in {CloseHours}hr",
            eventItem.Title,
            sessionStartTime,
            hoursUntilClose);
    }

    /// <summary>
    /// Creates a single ticket type for the edge case timing test event.
    /// This ticket should be OPEN when seeded (1 hour remaining in window),
    /// and will close after 1 hour passes.
    /// </summary>
    private void CreateEdgeCaseTimingTestTicketTypes(Event eventItem, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        var session = sessions.FirstOrDefault();
        if (session == null)
        {
            _logger.LogWarning("Edge case event {Title} missing session, cannot create ticket", eventItem.Title);
            return;
        }

        var closeHours = eventItem.RegistrationCloseHours ?? 24;
        var hoursUntilClose = 25 - (double)closeHours; // Session is 25hr away

        // Edge case ticket - should be OPEN (barely) when seeded
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            Sessions = new List<Session> { session },
            Name = "Edge Case Ticket",
            Description = $"Tests 1-hour-until-close edge case. Session: 25hr, Close: {closeHours}hr, Remaining: {hoursUntilClose}hr. Expected: {(hoursUntilClose > 0 ? "OPEN" : "CLOSED")}",
            Price = 25.00m,
            Available = 10,
            PricingType = PricingType.Fixed
        });

        _logger.LogInformation(
            "Created edge case ticket for {Title}: Status {Status} ({HoursRemaining}hr until close)",
            eventItem.Title,
            hoursUntilClose > 0 ? "OPEN" : "CLOSED",
            hoursUntilClose);
    }
}
