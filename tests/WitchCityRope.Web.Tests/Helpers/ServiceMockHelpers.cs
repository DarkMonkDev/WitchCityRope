using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using WitchCityRope.Web.Services;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Web.Tests.Helpers
{
    /// <summary>
    /// Helper methods for creating common service mocks
    /// </summary>
    public static class ServiceMockHelpers
    {
        /// <summary>
        /// Creates a mock event service with default data
        /// </summary>
        public static Mock<IEventService> CreateMockEventService(List<EventDto> events = null)
        {
            var mock = new Mock<IEventService>();
            
            events ??= CreateDefaultEvents();
            
            mock.Setup(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(events);
            
            mock.Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => events.Find(e => e.Id == id));
            
            mock.Setup(x => x.GetEventsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync((DateTime start, DateTime end) => 
                    events.FindAll(e => e.StartDateTime >= start && e.StartDateTime <= end));
            
            return mock;
        }

        /// <summary>
        /// Creates a mock registration service
        /// </summary>
        public static Mock<IRegistrationService> CreateMockRegistrationService()
        {
            var mock = new Mock<IRegistrationService>();
            
            mock.Setup(x => x.RegisterForEventAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new RegistrationResult { Success = true });
            
            mock.Setup(x => x.CancelRegistrationAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new RegistrationResult { Success = true });
            
            mock.Setup(x => x.GetUserRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationDto>());
            
            return mock;
        }

        /// <summary>
        /// Creates a mock notification service
        /// </summary>
        public static Mock<INotificationService> CreateMockNotificationService()
        {
            var mock = new Mock<INotificationService>();
            
            mock.Setup(x => x.ShowSuccessAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            mock.Setup(x => x.ShowErrorAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            mock.Setup(x => x.ShowWarningAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            mock.Setup(x => x.ShowInfoAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            return mock;
        }

        /// <summary>
        /// Creates default event data for testing
        /// </summary>
        private static List<EventDto> CreateDefaultEvents()
        {
            return new List<EventDto>
            {
                new EventDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Rope Basics Workshop",
                    Description = "Learn fundamental rope techniques",
                    StartDateTime = DateTime.UtcNow.AddDays(3),
                    EndDateTime = DateTime.UtcNow.AddDays(3).AddHours(2),
                    Location = "Studio A",
                    MaxAttendees = 15,
                    CurrentAttendees = 8,
                    Price = 50,
                    IsPublic = true,
                    Type = EventType.Workshop,
                    Status = EventStatus.Published
                },
                new EventDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced Suspension",
                    Description = "For experienced practitioners only",
                    StartDateTime = DateTime.UtcNow.AddDays(7),
                    EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
                    Location = "Main Hall",
                    MaxAttendees = 10,
                    CurrentAttendees = 10,
                    Price = 75,
                    IsPublic = true,
                    Type = EventType.Class,
                    Status = EventStatus.Published
                },
                new EventDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Monthly Social",
                    Description = "Community gathering and practice time",
                    StartDateTime = DateTime.UtcNow.AddDays(14),
                    EndDateTime = DateTime.UtcNow.AddDays(14).AddHours(4),
                    Location = "Community Center",
                    MaxAttendees = 50,
                    CurrentAttendees = 22,
                    Price = 0,
                    IsPublic = true,
                    Type = EventType.Social,
                    Status = EventStatus.Published
                }
            };
        }

        /// <summary>
        /// Mock for local storage service
        /// </summary>
        public static Mock<ILocalStorageService> CreateMockLocalStorageService()
        {
            var mock = new Mock<ILocalStorageService>();
            var storage = new Dictionary<string, object>();

            mock.Setup(x => x.GetItemAsync<It.IsAnyType>(It.IsAny<string>()))
                .Returns<string>((key) => 
                {
                    if (storage.TryGetValue(key, out var value))
                    {
                        return Task.FromResult(value);
                    }
                    return Task.FromResult<object>(null);
                });

            mock.Setup(x => x.SetItemAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((key, value) => storage[key] = value)
                .Returns(Task.CompletedTask);

            mock.Setup(x => x.RemoveItemAsync(It.IsAny<string>()))
                .Callback<string>((key) => storage.Remove(key))
                .Returns(Task.CompletedTask);

            mock.Setup(x => x.ClearAsync())
                .Callback(() => storage.Clear())
                .Returns(Task.CompletedTask);

            return mock;
        }

        /// <summary>
        /// Result class for registration operations
        /// </summary>
        public class RegistrationResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public Guid? RegistrationId { get; set; }
        }
    }
}