using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WitchCityRope.Core.Models;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.Services
{
    /// <summary>
    /// Service interface for managing event sessions and capacity calculations.
    /// Handles the Event Session Matrix concept where sessions are atomic capacity units.
    /// </summary>
    public interface IEventSessionService
    {
        /// <summary>
        /// Creates a new event with multiple sessions.
        /// </summary>
        Task<Result<EventDto>> CreateEventWithSessionsAsync(
            CreateEventRequest request, 
            CancellationToken ct = default);

        /// <summary>
        /// Gets an event with its sessions loaded.
        /// </summary>
        Task<Result<EventDto>> GetEventWithSessionsAsync(
            Guid eventId, 
            CancellationToken ct = default);

        /// <summary>
        /// Calculates available capacity for ticket types based on session constraints.
        /// </summary>
        Task<Result<List<SessionCapacityInfo>>> GetSessionCapacityAsync(
            Guid eventId, 
            CancellationToken ct = default);
    }

    /// <summary>
    /// Basic event creation request
    /// </summary>
    public class CreateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public Guid OrganizerId { get; set; }
    }

    /// <summary>
    /// Session capacity information
    /// </summary>
    public class SessionCapacityInfo
    {
        public string SessionIdentifier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int RegisteredCount { get; set; }
        public int AvailableSpots { get; set; }
    }
}