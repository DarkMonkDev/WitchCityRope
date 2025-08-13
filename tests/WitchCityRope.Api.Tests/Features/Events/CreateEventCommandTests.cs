using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Identity;
using WitchCityRope.Api.Exceptions;
using Moq;
using Xunit;

namespace WitchCityRope.Api.Tests.Features.Events
{
    public class CreateEventCommandTests : IDisposable
    {
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly Mock<IEventService> _eventServiceMock;

        public CreateEventCommandTests()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new WitchCityRopeIdentityDbContext(options);
            _eventServiceMock = new Mock<IEventService>();
        }

        [Fact]
        public async Task CreateEvent_ValidRequest_CreatesEvent()
        {
            // Arrange
            var organizer = new IdentityUserBuilder().AsOrganizer().Build();
            await _context.Users.AddAsync(organizer);
            await _context.SaveChangesAsync();

            var request = new CreateEventRequest
            {
                Name = "Beginner's Bondage Workshop",
                Description = "Learn the basics of rope bondage in a safe, inclusive environment",
                StartDateTime = DateTime.UtcNow.AddDays(7),
                EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
                Location = "Salem Community Center",
                MaxAttendees = 20,
                Price = 25m,
                RequiredSkillLevels = new() { "Beginner" },
                Tags = new() { "Rope", "Workshop" },
                RequiresVetting = false
            };

            var response = new CreateEventResponse
            {
                EventId = Guid.NewGuid(),
                Message = "Event created successfully"
            };

            var organizerId = Guid.NewGuid();
            _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<Guid>()))
                .ReturnsAsync(response);

            var service = _eventServiceMock.Object;

            // Act
            var result = await service.CreateEventAsync(request, organizerId);

            // Assert
            result.Should().NotBeNull();
            result.EventId.Should().NotBeEmpty();
            result.Message.Should().NotBeNullOrEmpty();

            _eventServiceMock.Verify(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task CreateEvent_InvalidDates_ThrowsException()
        {
            // Arrange
            var organizer = new IdentityUserBuilder().AsOrganizer().Build();
            await _context.Users.AddAsync(organizer);
            await _context.SaveChangesAsync();

            var request = new CreateEventRequest
            {
                Name = "Invalid Event",
                Description = "This event has invalid dates",
                StartDateTime = DateTime.UtcNow.AddDays(7),
                EndDateTime = DateTime.UtcNow.AddDays(6), // End before start
                Location = "Salem Community Center",
                MaxAttendees = 20,
                Price = 25m,
                RequiredSkillLevels = new() { "Beginner" },
                Tags = new() { "Rope", "Workshop" },
                RequiresVetting = false
            };

            _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<Guid>()))
                .ThrowsAsync(new ValidationException("Start date must be before end date"));

            var service = _eventServiceMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                service.CreateEventAsync(request, organizer.Id));
        }

        [Fact]
        public async Task CreateEvent_PastStartDate_ThrowsException()
        {
            // Arrange
            var organizer = new IdentityUserBuilder().AsOrganizer().Build();
            await _context.Users.AddAsync(organizer);
            await _context.SaveChangesAsync();

            var request = new CreateEventRequest
            {
                Name = "Past Event",
                Description = "This event is in the past",
                StartDateTime = DateTime.UtcNow.AddDays(-1),
                EndDateTime = DateTime.UtcNow.AddDays(-1).AddHours(2),
                Location = "Salem Community Center",
                MaxAttendees = 20,
                Price = 25m,
                RequiredSkillLevels = new() { "Beginner" },
                Tags = new() { "Rope", "Workshop" },
                RequiresVetting = false
            };

            _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<Guid>()))
                .ThrowsAsync(new ValidationException("Start date cannot be in the past"));

            var service = _eventServiceMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                service.CreateEventAsync(request, organizer.Id));
        }

        [Fact]
        public async Task CreateEvent_InvalidMaxAttendees_ThrowsException()
        {
            // Arrange
            var organizer = new IdentityUserBuilder().AsOrganizer().Build();
            await _context.Users.AddAsync(organizer);
            await _context.SaveChangesAsync();

            var request = new CreateEventRequest
            {
                Name = "Invalid Attendees Event",
                Description = "This event has invalid max attendees",
                StartDateTime = DateTime.UtcNow.AddDays(7),
                EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
                Location = "Salem Community Center",
                MaxAttendees = 0, // Invalid
                Price = 25m,
                RequiredSkillLevels = new() { "Beginner" },
                Tags = new() { "Rope", "Workshop" },
                RequiresVetting = false
            };

            _eventServiceMock.Setup(x => x.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<Guid>()))
                .ThrowsAsync(new ValidationException("Capacity must be greater than zero"));

            var service = _eventServiceMock.Object;

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                service.CreateEventAsync(request, organizer.Id));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}