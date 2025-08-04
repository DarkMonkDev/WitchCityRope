using System;
using FluentAssertions;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Tests.Common.Builders;
using Xunit;

namespace WitchCityRope.Tests.Common.Tests
{
    public class EventBuilderTests
    {
        [Fact]
        public void AllowPastDates_WhenCalled_ShouldCreateEventWithPastDates()
        {
            // Arrange
            var pastStartDate = DateTime.UtcNow.AddDays(-5);
            var pastEndDate = DateTime.UtcNow.AddDays(-4);
            
            // Act
            var eventBuilder = new EventBuilder()
                .WithTitle("Past Event")
                .WithStartDate(pastStartDate)
                .WithEndDate(pastEndDate)
                .AllowPastDates();
                
            var pastEvent = eventBuilder.Build();
            
            // Assert
            pastEvent.Should().NotBeNull();
            pastEvent.Title.Should().Be("Past Event");
            pastEvent.StartDate.Should().Be(pastStartDate);
            pastEvent.EndDate.Should().Be(pastEndDate);
        }
        
        [Fact]
        public void Build_WithPastDatesAndNoAllowFlag_ShouldThrowException()
        {
            // Arrange
            var pastStartDate = DateTime.UtcNow.AddDays(-5);
            var pastEndDate = DateTime.UtcNow.AddDays(-4);
            
            // Act
            var eventBuilder = new EventBuilder()
                .WithTitle("Past Event")
                .WithStartDate(pastStartDate)
                .WithEndDate(pastEndDate);
                // Note: NOT calling AllowPastDates()
            
            // Assert
            var act = () => eventBuilder.Build();
            act.Should().Throw<DomainException>()
                .WithMessage("Start date cannot be in the past");
        }
        
        [Fact]
        public void AllowPastDates_WithFutureDates_ShouldStillWork()
        {
            // Arrange
            var futureStartDate = DateTime.UtcNow.AddDays(5);
            var futureEndDate = DateTime.UtcNow.AddDays(6);
            
            // Act
            var eventBuilder = new EventBuilder()
                .WithTitle("Future Event")
                .WithStartDate(futureStartDate)
                .WithEndDate(futureEndDate)
                .AllowPastDates(); // Should not affect future dates
                
            var futureEvent = eventBuilder.Build();
            
            // Assert
            futureEvent.Should().NotBeNull();
            futureEvent.Title.Should().Be("Future Event");
            futureEvent.StartDate.Should().Be(futureStartDate);
            futureEvent.EndDate.Should().Be(futureEndDate);
        }
    }
}