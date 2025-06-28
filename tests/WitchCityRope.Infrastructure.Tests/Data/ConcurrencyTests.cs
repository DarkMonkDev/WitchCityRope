using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Tests.Fixtures;
using WitchCityRope.Tests.Common.Builders;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Data
{
    public class ConcurrencyTests : IntegrationTestBase
    {
        [Fact]
        public async Task Should_Handle_Optimistic_Concurrency_With_RowVersion()
        {
            // Arrange
            var user = new UserBuilder().Build();
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
            var @event = new EventBuilder()
                .WithCapacity(1) // Only 1 spot available
                .Build();
            
            var user1 = new UserBuilder().WithEmail("user1@example.com").Build();
            var user2 = new UserBuilder().WithEmail("user2@example.com").Build();

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
                        
                        var registration = new RegistrationBuilder()
                            .WithEvent(evt)
                            .WithUser(usr)
                            .Build();

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
            
            finalRegistrations.Should().BeLessOrEqualTo(@event.Capacity);
        }

        [Fact]
        public async Task Should_Handle_Concurrent_Payment_Processing()
        {
            // Arrange
            var registration = new RegistrationBuilder().Build();
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
            var applicant = new UserBuilder().Build();
            var reviewer1 = new UserBuilder().WithRole(UserRole.Administrator).Build();
            var reviewer2 = new UserBuilder().WithRole(UserRole.Administrator).Build();
            
            var application = new VettingApplication(
                applicant,
                "Intermediate",
                "Rope bondage, suspension",
                "Familiar with safety protocols and negotiation",
                new[] { "Reference from existing member" }
            );

            Context.Users.AddRange(applicant, reviewer1, reviewer2);
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
                        var reviewer = await context.Users.FindAsync(reviewerId);
                        var review = new VettingReview(reviewer, isApproved, $"Review by {reviewerId}");
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
            var @event = new EventBuilder()
                .WithTitle("Original Title")
                .Build();
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
            var user1 = new UserBuilder().WithEmail("user1@example.com").Build();
            var user2 = new UserBuilder().WithEmail("user2@example.com").Build();
            
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
            var users = new List<User>();
            for (int i = 0; i < 100; i++)
            {
                users.Add(new UserBuilder()
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
            var @event = new EventBuilder().Build();
            var users = new List<User>();
            
            for (int i = 0; i < 10; i++)
            {
                users.Add(new UserBuilder()
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
                
                var registration = new RegistrationBuilder()
                    .WithEvent(loadedEvent)
                    .WithUser(loadedUser)
                    .Build();
                
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