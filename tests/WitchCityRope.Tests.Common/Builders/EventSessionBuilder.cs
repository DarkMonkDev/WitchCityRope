using System;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Tests.Common.Builders
{
    /// <summary>
    /// Test builder for EventSession entity
    /// This represents the domain model we want to build during TDD Green phase
    /// </summary>
    public class EventSessionBuilder
    {
        private string _sessionName = "S1";
        private string _displayName = "Test Session";
        private DateTime _sessionDate = DateTime.UtcNow.AddDays(7);
        private TimeSpan _startTime = TimeSpan.FromHours(9);
        private TimeSpan _endTime = TimeSpan.FromHours(12);
        private int _capacity = 20;
        private bool _isRequired = false;

        public EventSessionBuilder WithSessionName(string sessionName)
        {
            _sessionName = sessionName;
            return this;
        }

        public EventSessionBuilder WithDisplayName(string displayName)
        {
            _displayName = displayName;
            return this;
        }

        public EventSessionBuilder WithSessionDate(DateTime sessionDate)
        {
            _sessionDate = sessionDate;
            return this;
        }

        public EventSessionBuilder WithStartTime(TimeSpan startTime)
        {
            _startTime = startTime;
            return this;
        }

        public EventSessionBuilder WithEndTime(TimeSpan endTime)
        {
            _endTime = endTime;
            return this;
        }

        public EventSessionBuilder WithCapacity(int capacity)
        {
            _capacity = capacity;
            return this;
        }

        public EventSessionBuilder AsRequired(bool isRequired = true)
        {
            _isRequired = isRequired;
            return this;
        }

        // Properties for validation in builder (before Build() is called)
        internal string SessionName => _sessionName;
        internal DateTime SessionDate => _sessionDate;
        internal TimeSpan StartTime => _startTime;
        internal TimeSpan EndTime => _endTime;

        public EventSession Build()
        {
            ValidateCapacity();
            ValidateTimes();

            return new EventSession
            {
                Id = Guid.NewGuid(),
                SessionName = _sessionName,
                DisplayName = _displayName,
                SessionDate = _sessionDate,
                StartTime = _startTime,
                EndTime = _endTime,
                Capacity = _capacity,
                IsRequired = _isRequired,
                RegisteredAttendees = 0, // Start with no registrations
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private void ValidateCapacity()
        {
            if (_capacity <= 0)
                throw new ArgumentException("Session capacity must be greater than zero", nameof(_capacity));
        }

        private void ValidateTimes()
        {
            if (_startTime >= _endTime)
                throw new ArgumentException("Session start time must be before end time");
        }
    }

    /// <summary>
    /// Domain entity representing Event Session (TDD target model)
    /// This is the structure the tests expect to exist after implementation
    /// </summary>
    public class EventSession
    {
        public Guid Id { get; set; }
        public string SessionName { get; set; } = null!; // S1, S2, S3, etc.
        public string DisplayName { get; set; } = null!; // "Friday Workshop", "Saturday Workshop"
        public DateTime SessionDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Capacity { get; set; }
        public bool IsRequired { get; set; } // For prerequisite handling
        public int RegisteredAttendees { get; set; } = 0; // Track current registrations
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int GetAvailableCapacity()
        {
            return Capacity - RegisteredAttendees;
        }

        public bool HasAvailableCapacity(int requestedQuantity = 1)
        {
            return GetAvailableCapacity() >= requestedQuantity;
        }
    }
}