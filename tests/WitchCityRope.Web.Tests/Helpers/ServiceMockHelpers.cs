using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using WitchCityRope.Web.Services;

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
        public static Mock<IEventService> CreateMockEventService(List<EventListItem> events = null)
        {
            var mock = new Mock<IEventService>();
            
            events ??= CreateDefaultEventListItems();
            
            mock.Setup(x => x.GetUpcomingEventsAsync())
                .ReturnsAsync(events);
            
            mock.Setup(x => x.GetEventDetailAsync(It.IsAny<Guid>()))
                .ReturnsAsync((EventDetail?)null);
            
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
            var mock = ServiceMockHelper.CreateNotificationServiceMock();
            return mock;
        }
        
        /// <summary>
        /// Creates a mock toast service
        /// </summary>
        public static Mock<IToastService> CreateMockToastService()
        {
            var mock = ServiceMockHelper.CreateToastServiceMock();
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
                    AvailableSpots = 28,
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
                .Returns(new InvocationFunc(invocation =>
                {
                    var key = (string)invocation.Arguments[0];
                    if (storage.TryGetValue(key, out var value))
                    {
                        var genericType = invocation.Method.GetGenericArguments()[0];
                        var taskType = typeof(Task<>).MakeGenericType(genericType);
                        var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(genericType);
                        return fromResultMethod.Invoke(null, new[] { value });
                    }
                    else
                    {
                        var genericType = invocation.Method.GetGenericArguments()[0];
                        var taskType = typeof(Task<>).MakeGenericType(genericType);
                        var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(genericType);
                        return fromResultMethod.Invoke(null, new[] { genericType.IsValueType ? Activator.CreateInstance(genericType) : null });
                    }
                }));

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

    }
}