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
            Capacity = eventItem.Capacity,
            CurrentAttendees = eventItem.GetCurrentAttendeeCount()
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
            Capacity = eventItem.Capacity / 2,
            CurrentAttendees = 0
        };

        // Day 2: 8:00 PM - 10:00 PM
        var day2Session = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "DAY2",
            Name = "Day 2",
            StartTime = eventItem.StartDate.AddHours(2),
            EndTime = eventItem.StartDate.AddHours(4),
            Capacity = eventItem.Capacity / 2,
            CurrentAttendees = 0
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
                Capacity = (int)Math.Ceiling(eventItem.Capacity / (double)numberOfDays),
                CurrentAttendees = eventItem.GetCurrentAttendeeCount() / numberOfDays
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
                SessionId = session.Id,
                Name = "Free RSVP",
                Description = "Free attendance - RSVP required",
                Price = 0,
                Available = session.Capacity,
                Sold = eventItem.GetCurrentRSVPCount(),
                PricingType = PricingType.Fixed
            };

            var donationTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = session.Id,
                Name = "Support Donation",
                Description = "Optional sliding scale donation to support the community - pay what you can!",
                Price = null, // Not used for sliding scale
                MinPrice = 10m,
                MaxPrice = 40m,
                DefaultPrice = 20m,
                Available = session.Capacity,
                Sold = eventItem.GetCurrentTicketCount(),
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
                SessionId = session.Id,
                Name = "Regular",
                Description = "Full access to the workshop",
                Price = price,
                Available = session.Capacity,
                Sold = eventItem.GetCurrentAttendeeCount(),
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
                    SessionId = session.Id,
                    Name = $"Day {i + 1} RSVP",
                    Description = $"Free RSVP for Day {i + 1} only",
                    Price = 0,
                    Available = session.Capacity,
                    Sold = (int)(session.Capacity * 0.3), // 30% sold on average
                    PricingType = PricingType.Fixed
                };

                ticketTypesToAdd.Add(rsvpTicket);
            }

            // Full event sliding scale donation
            var fullEventTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = null, // Multi-session ticket
                Name = $"All {sessions.Count} Days Support",
                Description = $"Optional sliding scale donation for all {sessions.Count} days - pay what you can!",
                Price = null, // Not used for sliding scale
                MinPrice = 10m,
                MaxPrice = 40m,
                DefaultPrice = 20m,
                Available = eventItem.Capacity,
                Sold = eventItem.GetCurrentAttendeeCount(),
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
                    SessionId = session.Id,
                    Name = $"Day {i + 1} Only",
                    Description = $"Access to Day {i + 1} activities only",
                    Price = dailyPrice,
                    Available = session.Capacity,
                    Sold = (int)(session.Capacity * 0.3), // 30% sold on average
                    PricingType = PricingType.Fixed
                };

                ticketTypesToAdd.Add(dayTicket);
            }

            // Create full event ticket with discount
            var fullEventTicket = new TicketType
            {
                EventId = eventItem.Id,
                SessionId = null, // Multi-session ticket
                Name = $"All {sessions.Count} Days",
                Description = $"Full access to all {sessions.Count} days - SAVE ${(dailyPrice * sessions.Count - basePrice):F0}!",
                Price = basePrice,
                Available = eventItem.Capacity,
                Sold = eventItem.GetCurrentAttendeeCount(),
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
            Capacity = eventItem.Capacity,
            CurrentAttendees = 0
        };

        // Session 2: Afternoon Session - Advanced (1 PM - 4 PM)
        var afternoonSession = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "AFTERNOON",
            Name = "Afternoon Session - Advanced",
            StartTime = eventItem.StartDate.AddHours(4), // 1 PM
            EndTime = eventItem.StartDate.AddHours(7),   // 4 PM
            Capacity = eventItem.Capacity,
            CurrentAttendees = 0
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
            Capacity = eventItem.Capacity,
            CurrentAttendees = 0
        };

        // Session 2: Core Ties (12 PM - 2:30 PM)
        var coreTiesSession = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "CORE",
            Name = "Core Ties",
            StartTime = eventItem.StartDate.AddHours(3),
            EndTime = eventItem.StartDate.AddHours(5.5),
            Capacity = eventItem.Capacity,
            CurrentAttendees = 0
        };

        // Session 3: Practice and Review (3 PM - 5:30 PM)
        var practiceSession = new Session
        {
            EventId = eventItem.Id,
            SessionCode = "PRACTICE",
            Name = "Practice and Review",
            StartTime = eventItem.StartDate.AddHours(6),
            EndTime = eventItem.StartDate.AddHours(8.5),
            Capacity = eventItem.Capacity,
            CurrentAttendees = 0
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
            SessionId = null, // Multi-session ticket
            Name = "Full Workshop Pass",
            Description = "Access to both morning and afternoon sessions",
            Price = 80.00m,
            Available = 15,
            Sold = 0, // Will be set by ticket purchases
            PricingType = PricingType.Fixed
        });

        // Single Session Ticket - $45
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            SessionId = null, // Can be used for either session
            Name = "Single Session Ticket",
            Description = "Access to one session (morning or afternoon)",
            Price = 45.00m,
            Available = 10,
            Sold = 0,
            PricingType = PricingType.Fixed
        });

        // Early Bird Full Pass - $65
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            SessionId = null,
            Name = "Early Bird Full Pass",
            Description = "Discounted full workshop access",
            Price = 65.00m,
            Available = 5,
            Sold = 0,
            PricingType = PricingType.Fixed
        });
    }

    /// <summary>
    /// Creates ticket types for Rope Fundamentals Intensive historical event.
    /// Full Day Pass ($100), Half Day Pass ($60), Single Session ($35).
    /// </summary>
    private void CreateRopeFundamentalsIntensiveTicketTypes(Event eventItem, List<Session> sessions, List<TicketType> ticketTypesToAdd)
    {
        // Full Day Pass - $100
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            SessionId = null, // Multi-session ticket
            Name = "Full Day Pass",
            Description = "Access to all three sessions",
            Price = 100.00m,
            Available = 20,
            Sold = 0,
            PricingType = PricingType.Fixed
        });

        // Half Day Pass - $60 (any 2 sessions)
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            SessionId = null,
            Name = "Half Day Pass",
            Description = "Access to any two sessions",
            Price = 60.00m,
            Available = 10,
            Sold = 0,
            PricingType = PricingType.Fixed
        });

        // Single Session - $35
        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            SessionId = null,
            Name = "Single Session",
            Description = "Access to one session",
            Price = 35.00m,
            Available = 5,
            Sold = 0,
            PricingType = PricingType.Fixed
        });
    }

    /// <summary>
    /// Creates ticket types for social historical events (Practice Night, Welcome Mixer).
    /// Suggested Donation with sliding scale pricing.
    /// </summary>
    private void CreateSocialEventTicketTypes(Event eventItem, Session session, List<TicketType> ticketTypesToAdd)
    {
        // Suggested Donation - sliding scale
        var suggestedPrice = eventItem.Title.Contains("Practice Night") ? 10.00m : 5.00m;

        ticketTypesToAdd.Add(new TicketType
        {
            EventId = eventItem.Id,
            SessionId = session.Id,
            Name = "Suggested Donation",
            Description = eventItem.Title.Contains("Practice Night")
                ? "Optional donation to support the space"
                : "Optional donation to support refreshments",
            Price = suggestedPrice,
            MinPrice = 0m,
            MaxPrice = suggestedPrice * 4,
            DefaultPrice = suggestedPrice,
            Available = eventItem.Capacity,
            Sold = 0,
            PricingType = PricingType.SlidingScale
        });
    }
}
