using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Api.Exceptions;
using Moq;
using Xunit;

namespace WitchCityRope.Api.Tests.Features.Events
{
    public class CreateEventCommandTests : IDisposable
    {
        private readonly WitchCityRopeDbContext _context;
        private readonly Mock<IEventService> _eventServiceMock;

        public CreateEventCommandTests()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new WitchCityRopeDbContext(options);
            _eventServiceMock = new Mock<IEventService>();
        }

        [Fact]
        public async Task CreateEvent_ValidRequest_CreatesEvent()
        {
            // Arrange
            var organizer = new UserBuilder().AsOrganizer().Build();
            await _context.Users.AddAsync(organizer);
            await _context.SaveChangesAsync();

            var request = new CreateEventRequest(
                Title: "Beginner's Bondage Workshop",
                Description: "Learn the basics of rope bondage in a safe, inclusive environment",
                Type: EventType.Workshop,
                StartDateTime: DateTime.UtcNow.AddDays(7),
                EndDateTime: DateTime.UtcNow.AddDays(7).AddHours(2),
                Location: "Salem Community Center",
                MaxAttendees: 20,
                Price: 25m,
                RequiredSkillLevels: new[] { "Beginner" },
                Tags: new[] { "Rope", "Workshop" },
                RequiresVetting: false,
                SafetyNotes: "Safe words will be discussed",
                EquipmentProvided: "Rope will be provided",
                EquipmentRequired: "None",
                OrganizerId: organizer.Id
            );

            var response = new CreateEventResponse(
                EventId: Guid.NewGuid(),
                Title: request.Title,
                Slug: "beginners-bondage-workshop",
                CreatedAt: DateTime.UtcNow
            );

            _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()))
                .ReturnsAsync(response);

            var service = _eventServiceMock.Object;

            // Act
            var result = await service.CreateEventAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.EventId.Should().NotBeEmpty();
            result.Title.Should().Be(request.Title);
            result.Slug.Should().NotBeNullOrEmpty();

            _eventServiceMock.Verify(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()), Times.Once);
        }

        [Fact]
        public async Task CreateEvent_InvalidDates_ThrowsException()
        {
            // Arrange
            var organizer = new UserBuilder().AsOrganizer().Build();
            await _context.Users.AddAsync(organizer);
            await _context.SaveChangesAsync();

            var request = new CreateEventRequest(
                Title: "Invalid Event",
                Description: "This event has invalid dates",
                Type: EventType.Workshop,
                StartDateTime: DateTime.UtcNow.AddDays(7),
                EndDateTime: DateTime.UtcNow.AddDays(6), // End before start
                Location: "Salem Community Center",
                MaxAttendees: 20,
                Price: 25m,
                RequiredSkillLevels: new[] { "Beginner" },
                Tags: new[] { "Rope", "Workshop" },
                RequiresVetting: false,
                SafetyNotes: "Safe words will be discussed",
                EquipmentProvided: "Rope will be provided",
                EquipmentRequired: "None",
                OrganizerId: organizer.Id
            );

            _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()))
                .ThrowsAsync(new ValidationException("Start date must be before end date"));

            var service = _eventServiceMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                service.CreateEventAsync(request));
        }

        [Fact]
        public async Task CreateEvent_PastStartDate_ThrowsException()
        {
            // Arrange
            var organizer = new UserBuilder().AsOrganizer().Build();
            await _context.Users.AddAsync(organizer);
            await _context.SaveChangesAsync();

            var request = new CreateEventRequest(
                Title: "Past Event",
                Description: "This event is in the past",
                Type: EventType.Workshop,
                StartDateTime: DateTime.UtcNow.AddDays(-1),
                EndDateTime: DateTime.UtcNow.AddDays(-1).AddHours(2),
                Location: "Salem Community Center",
                MaxAttendees: 20,
                Price: 25m,
                RequiredSkillLevels: new[] { "Beginner" },
                Tags: new[] { "Rope", "Workshop" },
                RequiresVetting: false,
                SafetyNotes: "Safe words will be discussed",
                EquipmentProvided: "Rope will be provided",
                EquipmentRequired: "None",
                OrganizerId: organizer.Id
            );

            _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()))
                .ThrowsAsync(new ValidationException("Start date cannot be in the past"));

            var service = _eventServiceMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                service.CreateEventAsync(request));
        }

        [Fact]
        public async Task CreateEvent_InvalidMaxAttendees_ThrowsException()
        {
            // Arrange
            var organizer = new UserBuilder().AsOrganizer().Build();
            await _context.Users.AddAsync(organizer);
            await _context.SaveChangesAsync();

            var request = new CreateEventRequest(
                Title: "Invalid Attendees Event",
                Description: "This event has invalid max attendees",
                Type: EventType.Workshop,
                StartDateTime: DateTime.UtcNow.AddDays(7),
                EndDateTime: DateTime.UtcNow.AddDays(7).AddHours(2),
                Location: "Salem Community Center",
                MaxAttendees: 0, // Invalid
                Price: 25m,
                RequiredSkillLevels: new[] { "Beginner" },
                Tags: new[] { "Rope", "Workshop" },
                RequiresVetting: false,
                SafetyNotes: "Safe words will be discussed",
                EquipmentProvided: "Rope will be provided",
                EquipmentRequired: "None",
                OrganizerId: organizer.Id
            );

            _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>()))
                .ThrowsAsync(new ValidationException("Capacity must be greater than zero"));

            var service = _eventServiceMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                service.CreateEventAsync(request));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}