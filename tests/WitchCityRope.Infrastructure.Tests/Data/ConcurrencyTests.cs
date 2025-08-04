using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Infrastructure.Tests.Fixtures;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Identity;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Data
{
    public class ConcurrencyTests : IntegrationTestBase
    {
        private Event CreateTestEvent(string title = "Test Event", int capacity = 10)
        {
            var eventType = typeof(Event);
            var eventCtor = eventType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var @event = (Event)eventCtor.Invoke(null);
            
            // Use reflection to set properties
            eventType.GetProperty("Id").SetValue(@event, Guid.NewGuid());
            eventType.GetProperty("Title").SetValue(@event, title);
            eventType.GetProperty("Description").SetValue(@event, "Test event description");
            eventType.GetProperty("StartDate").SetValue(@event, DateTime.UtcNow.AddDays(7));
            eventType.GetProperty("EndDate").SetValue(@event, DateTime.UtcNow.AddDays(7).AddHours(3));
            eventType.GetProperty("Capacity").SetValue(@event, capacity);
            eventType.GetProperty("EventType").SetValue(@event, EventType.Workshop);
            eventType.GetProperty("Location").SetValue(@event, "Test Location");
            eventType.GetProperty("IsPublished").SetValue(@event, true);
            eventType.GetProperty("CreatedAt").SetValue(@event, DateTime.UtcNow);
            eventType.GetProperty("UpdatedAt").SetValue(@event, DateTime.UtcNow);

            return @event;
        }

        private Registration CreateTestRegistration(Guid userId, Guid eventId)
        {
            var regType = typeof(Registration);
            var regCtor = regType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var registration = (Registration)regCtor.Invoke(null);
            
            regType.GetProperty("Id").SetValue(registration, Guid.NewGuid());
            regType.GetProperty("UserId").SetValue(registration, userId);
            regType.GetProperty("EventId").SetValue(registration, eventId);
            regType.GetProperty("Status").SetValue(registration, RegistrationStatus.Confirmed);
            regType.GetProperty("SelectedPrice").SetValue(registration, Money.Create(50m));
            regType.GetProperty("RegisteredAt").SetValue(registration, DateTime.UtcNow);

            return registration;
        }

        private VettingApplication CreateTestVettingApplication(Guid applicantId)
        {
            var appType = typeof(VettingApplication);
            var appCtor = appType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var application = (VettingApplication)appCtor.Invoke(null);
            
            appType.GetProperty("Id").SetValue(application, Guid.NewGuid());
            appType.GetProperty("ApplicantId").SetValue(application, applicantId);
            appType.GetProperty("ExperienceLevel").SetValue(application, "Intermediate");
            appType.GetProperty("Interests").SetValue(application, "Rope bondage, suspension");
            appType.GetProperty("SafetyKnowledge").SetValue(application, "First aid certified");
            appType.GetProperty("Status").SetValue(application, VettingStatus.Submitted);
            appType.GetProperty("SubmittedAt").SetValue(application, DateTime.UtcNow);

            return application;
        }

        private VettingReview CreateTestVettingReview(Guid reviewerId, bool recommendation = true)
        {
            var reviewType = typeof(VettingReview);
            var reviewCtor = reviewType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            var review = (VettingReview)reviewCtor.Invoke(null);
            
            reviewType.GetProperty("Id").SetValue(review, Guid.NewGuid());
            reviewType.GetProperty("ReviewerId").SetValue(review, reviewerId);
            reviewType.GetProperty("Recommendation").SetValue(review, recommendation);
            reviewType.GetProperty("Notes").SetValue(review, $"Review by {reviewerId}");
            reviewType.GetProperty("ReviewedAt").SetValue(review, DateTime.UtcNow);

            return review;
        }
        [Fact]
        public async Task Should_Handle_Optimistic_Concurrency_With_RowVersion()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act & Assert
            using var context1 = CreateNewContext();
            using var context2 = CreateNewContext();

            // Both contexts load the same user
            var user1 = await context1.Users.FirstAsync(u => u.Id == user.Id);
            var user2 = await context2.Users.FirstAsync(u => u.Id == user.Id);

            // Context 1 updates and saves
            user1.UpdateSceneName(SceneName.Create("UpdatedByContext1"));
            await context1.SaveChangesAsync();

            // Context 2 tries to update the same user
            user2.UpdateSceneName(SceneName.Create("UpdatedByContext2"));

            // This should throw concurrency exception
            var act = async () => await context2.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task Should_Handle_Concurrent_Event_Registration()
        {
            // Arrange
            var @event = CreateTestEvent("Limited Capacity Event", 1); // Only 1 spot available
            
            var user1 = new IdentityUserBuilder().WithEmail("user1@example.com").Build();
            var user2 = new IdentityUserBuilder().WithEmail("user2@example.com").Build();

            Context.Events.Add(@event);
            Context.Users.AddRange(user1, user2);
            await Context.SaveChangesAsync();

            // Act
            var tasks = new List<Task<bool>>();

            // Simulate two users trying to register at the same time
            for (int i = 0; i < 2; i++)
            {
                var userId = i == 0 ? user1.Id : user2.Id;
                var task = Task.Run(async () =>
                {
                    using var context = CreateNewContext();
                    var evt = await context.Events
                        .Include(e => e.Registrations)
                        .FirstAsync(e => e.Id == @event.Id);

                    // Check if spots available
                    if (evt.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed) < evt.Capacity)
                    {
                        var usr = await context.Users.FindAsync(userId);
                        if (usr == null) 
                        {
                            return false; // Skip if user not found
                        }
                        
                        var registration = CreateTestRegistration(usr.Id, evt.Id);

                        context.Registrations.Add(registration);
                        
                        try
                        {
                            await context.SaveChangesAsync();
                            return true;
                        }
                        catch (DbUpdateException)
                        {
                            return false;
                        }
                    }
                    return false;
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            // At least one should succeed, but not both due to capacity constraint
            results.Count(r => r).Should().BeGreaterThanOrEqualTo(1);
            
            // Verify final state
            var finalRegistrations = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Registrations.CountAsync(r => r.EventId == @event.Id));
            
            finalRegistrations.Should().BeLessThanOrEqualTo(@event.Capacity);
        }

        [Fact]
        public async Task Should_Handle_Concurrent_Payment_Processing()
        {
            // Arrange
            var user = new IdentityUserBuilder().Build();
            var @event = CreateTestEvent();
            Context.Users.Add(user);
            Context.Events.Add(@event);
            await Context.SaveChangesAsync();
            
            var registration = CreateTestRegistration(user.Id, @event.Id);
            Context.Registrations.Add(registration);
            await Context.SaveChangesAsync();

            // Act
            var tasks = new List<Task<bool>>();

            // Simulate multiple payment attempts for the same registration
            for (int i = 0; i < 3; i++)
            {
                var task = Task.Run(async () =>
                {
                    using var context = CreateNewContext();
                    var reg = await context.Registrations
                        .Include(r => r.Payment)
                        .FirstAsync(r => r.Id == registration.Id);

                    // Check if payment already exists
                    if (reg.Payment == null)
                    {
                        var payment = new PaymentBuilder()
                            .WithRegistration(reg)
                            .Build();

                        context.Payments.Add(payment);
                        
                        try
                        {
                            await context.SaveChangesAsync();
                            return true;
                        }
                        catch (DbUpdateException)
                        {
                            return false;
                        }
                    }
                    return false;
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            // Only one payment should be created
            results.Count(r => r).Should().Be(1);
            
            var finalPayments = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Payments.CountAsync(p => p.RegistrationId == registration.Id));
            
            finalPayments.Should().Be(1);
        }

        [Fact]
        public async Task Should_Handle_Concurrent_Vetting_Application_Reviews()
        {
            // Arrange
            var applicant = new IdentityUserBuilder().Build();
            var reviewer1 = new IdentityUserBuilder().WithRole(UserRole.Administrator).Build();
            var reviewer2 = new IdentityUserBuilder().WithRole(UserRole.Administrator).Build();
            
            Context.Users.AddRange(applicant, reviewer1, reviewer2);
            await Context.SaveChangesAsync();

            var application = CreateTestVettingApplication(applicant.Id);
            Context.VettingApplications.Add(application);
            await Context.SaveChangesAsync();

            // Act
            var tasks = new List<Task<bool>>();

            // Two admins try to review the same application simultaneously
            for (int i = 0; i < 2; i++)
            {
                var reviewerId = i == 0 ? reviewer1.Id : reviewer2.Id;
                var isApproved = i == 0;
                
                var task = Task.Run(async () =>
                {
                    using var context = CreateNewContext();
                    var app = await context.VettingApplications
                        .FirstAsync(va => va.Id == application.Id);

                    if (app.Status == VettingStatus.Submitted)
                    {
                        app.StartReview();
                    }
                    
                    if (app.Status == VettingStatus.UnderReview)
                    {
                        var review = CreateTestVettingReview(reviewerId, isApproved);
                        app.AddReview(review);
                        
                        try
                        {
                            await context.SaveChangesAsync();
                            return true;
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            return false;
                        }
                    }
                    return false;
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            // Only one review should succeed
            results.Count(r => r).Should().Be(1);
            
            var finalApp = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.VettingApplications.FirstAsync(va => va.Id == application.Id));
            
            finalApp.Status.Should().Be(VettingStatus.UnderReview);
            finalApp.Reviews.Should().HaveCount(1);
        }

        [Fact]
        public async Task Should_Handle_Concurrent_Updates_With_Retry_Logic()
        {
            // Arrange
            var @event = CreateTestEvent("Original Title");
            Context.Events.Add(@event);
            await Context.SaveChangesAsync();

            // Act
            var tasks = new List<Task<string>>();

            for (int i = 0; i < 5; i++)
            {
                var updateNumber = i;
                var task = Task.Run(async () =>
                {
                    const int maxRetries = 3;
                    int attempts = 0;
                    
                    while (attempts < maxRetries)
                    {
                        attempts++;
                        using var context = CreateNewContext();
                        
                        try
                        {
                            var evt = await context.Events.FirstAsync(e => e.Id == @event.Id);
                            evt.UpdateDetails(
                                $"Updated Title {updateNumber}",
                                evt.Description,
                                evt.Location
                            );
                            
                            await context.SaveChangesAsync();
                            return evt.Title;
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            if (attempts >= maxRetries)
                                throw;
                            
                            // Wait a bit before retry
                            await Task.Delay(Random.Shared.Next(10, 50));
                        }
                    }
                    
                    return "Failed";
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            // All tasks should eventually succeed with retry logic
            results.Should().NotContain("Failed");
            results.Should().OnlyContain(title => title.StartsWith("Updated Title"));
            
            // Verify final state
            var finalEvent = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Events.FirstAsync(e => e.Id == @event.Id));
            
            finalEvent.Title.Should().StartWith("Updated Title");
        }

        [Fact]
        public async Task Should_Handle_Deadlock_Scenario()
        {
            // Arrange
            var user1 = new IdentityUserBuilder().WithEmail("user1@example.com").Build();
            var user2 = new IdentityUserBuilder().WithEmail("user2@example.com").Build();
            
            Context.Users.AddRange(user1, user2);
            await Context.SaveChangesAsync();

            // Act
            var task1 = Task.Run(async () =>
            {
                using var context = CreateNewContext();
                using var transaction = await context.Database.BeginTransactionAsync();
                
                try
                {
                    // Update user1 first
                    var u1 = await context.Users.FirstAsync(u => u.Id == user1.Id);
                    u1.UpdateSceneName(SceneName.Create("Task1User1"));
                    await context.SaveChangesAsync();
                    
                    // Small delay to increase chance of deadlock
                    await Task.Delay(50);
                    
                    // Then update user2
                    var u2 = await context.Users.FirstAsync(u => u.Id == user2.Id);
                    u2.UpdateSceneName(SceneName.Create("Task1User2"));
                    await context.SaveChangesAsync();
                    
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            });

            var task2 = Task.Run(async () =>
            {
                using var context = CreateNewContext();
                using var transaction = await context.Database.BeginTransactionAsync();
                
                try
                {
                    // Update user2 first (opposite order)
                    var u2 = await context.Users.FirstAsync(u => u.Id == user2.Id);
                    u2.UpdateSceneName(SceneName.Create("Task2User2"));
                    await context.SaveChangesAsync();
                    
                    // Small delay to increase chance of deadlock
                    await Task.Delay(50);
                    
                    // Then update user1
                    var u1 = await context.Users.FirstAsync(u => u.Id == user1.Id);
                    u1.UpdateSceneName(SceneName.Create("Task2User1"));
                    await context.SaveChangesAsync();
                    
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            });

            var results = await Task.WhenAll(task1, task2);

            // Assert
            // At least one should succeed (the other might fail due to deadlock)
            results.Should().Contain(true);
        }

        [Fact]
        public async Task Should_Handle_Parallel_Bulk_Operations()
        {
            // Arrange
            var users = new List<WitchCityRopeUser>();
            for (int i = 0; i < 100; i++)
            {
                users.Add(new IdentityUserBuilder()
                    .WithEmail($"user{i}@example.com")
                    .WithSceneName($"User{i}")
                    .Build());
            }

            // Act
            var tasks = new List<Task>();

            // Split users into batches and insert in parallel
            var batches = users.Chunk(25).ToList();
            foreach (var batch in batches)
            {
                var task = Task.Run(async () =>
                {
                    using var context = CreateNewContext();
                    context.Users.AddRange(batch);
                    await context.SaveChangesAsync();
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert
            var totalUsers = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Users.CountAsync());
            
            totalUsers.Should().Be(100);
        }

        [Fact]
        public async Task Should_Handle_Concurrent_Aggregate_Updates()
        {
            // Arrange
            var @event = CreateTestEvent();
            var users = new List<WitchCityRopeUser>();
            
            for (int i = 0; i < 10; i++)
            {
                users.Add(new IdentityUserBuilder()
                    .WithEmail($"user{i}@example.com")
                    .Build());
            }

            Context.Events.Add(@event);
            Context.Users.AddRange(users);
            await Context.SaveChangesAsync();

            // Act
            var tasks = users.Select(user => Task.Run(async () =>
            {
                using var context = CreateNewContext();
                
                // Each user tries to register for the event
                var loadedEvent = await context.Events.FindAsync(@event.Id);
                var loadedUser = await context.Users.FindAsync(user.Id);
                
                if (loadedEvent == null || loadedUser == null)
                {
                    return false; // Skip if event or user not found
                }
                
                var registration = CreateTestRegistration(loadedUser.Id, loadedEvent.Id);
                
                context.Registrations.Add(registration);
                
                try
                {
                    await context.SaveChangesAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            })).ToList();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.All(r => r).Should().BeTrue(); // All registrations should succeed
            
            var totalRegistrations = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Registrations.CountAsync(r => r.EventId == @event.Id));
            
            totalRegistrations.Should().Be(10);
        }
    }
}