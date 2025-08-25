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

            // Use actual Core.Entities.EventSession
            return new EventSession(
                eventId: Guid.NewGuid(), // Temporary ID for tests
                sessionIdentifier: _sessionName,
                name: _displayName,
                date: _sessionDate,
                startTime: _startTime,
                endTime: _endTime,
                capacity: _capacity,
                isRequired: _isRequired);
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
    // NOTE: This test model will be replaced by the actual Core.Entities.EventSession during implementation
}