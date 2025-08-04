using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using WitchCityRope.Web.Services;
using WitchCityRope.Core.DTOs;
using CoreEnums = WitchCityRope.Core.Enums;

namespace WitchCityRope.Web.Tests.New.Helpers
{
    /// <summary>
    /// Helper methods for creating common service mocks
    /// </summary>
    public static class ServiceMockHelpers
    {
        /// <summary>
        /// Creates a mock event service with default data
        /// </summary>
        public static Mock<IEventService> CreateMockEventService(List<EventListItem> events = null)
        {
            var mock = new Mock<IEventService>();
            
            events ??= CreateDefaultEventListItems();
            
            mock.Setup(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(events);
            
            mock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => {
                    var item = events.Find(e => e.Id == id);
                    if (item == null) return null;
                    return new EventDetail
                    {
                        Id = item.Id,
                        Title = item.Title,
                        StartDateTime = item.StartDateTime,
                        Location = item.Location,
                        AvailableSpots = item.AvailableSpots,
                        Price = item.Price,
                        Description = "Test description",
                        EndDateTime = item.StartDateTime.AddHours(2),
                        IsRegistered = false
                    };
                });
            
            mock.Setup(x => x.RegisterForEventAsync(It.IsAny<Guid>()))
                .ReturnsAsync(true);
            
            return mock;
        }

        /// <summary>
        /// Creates a mock registration service
        /// </summary>
        public static Mock<IRegistrationService> CreateMockRegistrationService()
        {
            var mock = new Mock<IRegistrationService>();
            
            mock.Setup(x => x.GetMyRegistrationsAsync())
                .ReturnsAsync(new List<UserRegistration>());
            
            mock.Setup(x => x.CancelRegistrationAsync(It.IsAny<Guid>()))
                .ReturnsAsync(true);
            
            return mock;
        }

        /// <summary>
        /// Creates a mock notification service
        /// </summary>
        public static Mock<INotificationService> CreateMockNotificationService()
        {
            var mock = new Mock<INotificationService>();
            
            mock.Setup(x => x.GetNotificationsAsync())
                .ReturnsAsync(new List<UserNotification>());
            
            mock.Setup(x => x.MarkNotificationAsReadAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
            
            return mock;
        }
        
        /// <summary>
        /// Creates a mock toast service
        /// </summary>
        public static Mock<IToastService> CreateMockToastService()
        {
            var mock = new Mock<IToastService>();
            var messages = new List<ToastMessage>();
            
            mock.SetupGet(x => x.Messages)
                .Returns(messages.AsReadOnly());
            
            mock.Setup(x => x.ShowSuccess(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(new ToastMessage { Message = msg, Level = ToastLevel.Success }));
            
            mock.Setup(x => x.ShowError(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(new ToastMessage { Message = msg, Level = ToastLevel.Error }));
            
            mock.Setup(x => x.ShowWarning(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(new ToastMessage { Message = msg, Level = ToastLevel.Warning }));
            
            mock.Setup(x => x.ShowInfo(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(new ToastMessage { Message = msg, Level = ToastLevel.Info }));
            
            mock.Setup(x => x.Remove(It.IsAny<Guid>()))
                .Callback<Guid>(id => messages.RemoveAll(m => m.Id == id));
            
            return mock;
        }

        /// <summary>
        /// Creates default event data for testing
        /// </summary>
        private static List<EventListItem> CreateDefaultEventListItems()
        {
            return new List<EventListItem>
            {
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Rope Basics Workshop",
                    StartDateTime = DateTime.UtcNow.AddDays(3),
                    Location = "Studio A",
                    AvailableSpots = 7,
                    Price = 50
                },
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced Suspension",
                    StartDateTime = DateTime.UtcNow.AddDays(7),
                    Location = "Main Hall",
                    AvailableSpots = 0,
                    Price = 75
                },
                new EventListItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Monthly Social",
                    StartDateTime = DateTime.UtcNow.AddDays(14),
                    Location = "Community Center",
                    AvailableSpots = 30,
                    Price = 0
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