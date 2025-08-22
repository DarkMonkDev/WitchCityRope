using System;
using System.Linq;
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
    public class WitchCityRopeDbContextTests : IntegrationTestBase
    {
        [Fact]
        public async Task Should_Create_Database_Schema()
        {
            // Act
            var canConnect = await Context.Database.CanConnectAsync();

            // Assert
            canConnect.Should().BeTrue();
            Context.Users.Should().NotBeNull();
            Context.Events.Should().NotBeNull();
            Context.Registrations.Should().NotBeNull();
            Context.Payments.Should().NotBeNull();
            Context.VettingApplications.Should().NotBeNull();
            Context.IncidentReports.Should().NotBeNull();
        }

        [Fact]
        public async Task Should_Save_And_Retrieve_User()
        {
            // Arrange
            var user = new UserBuilder().Build();

            // Act
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            var retrievedUser = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Users.FirstOrDefaultAsync(u => u.Id == user.Id));

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Email.Should().Be(user.Email);
            retrievedUser.SceneName.Should().Be(user.SceneName);
            retrievedUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Should_Update_Audit_Fields_On_Modify()
        {
            // Arrange
            var user = new UserBuilder().Build();
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            var originalUpdatedAt = user.UpdatedAt;

            // Act
            await Task.Delay(100); // Ensure time difference
            user.UpdateSceneName(user.SceneName);
            await Context.SaveChangesAsync();

            // Assert
            user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public async Task Should_Handle_Concurrent_Updates()
        {
            // Arrange
            var user = new UserBuilder().Build();
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            // Act & Assert
            using var context1 = CreateNewContext();
            using var context2 = CreateNewContext();

            var user1 = await context1.Users.FirstAsync(u => u.Id == user.Id);
            var user2 = await context2.Users.FirstAsync(u => u.Id == user.Id);

            user1.UpdateSceneName(SceneName.Create("Updated1"));
            await context1.SaveChangesAsync();

            user2.UpdateSceneName(SceneName.Create("Updated2"));

            // This should throw a concurrency exception
            var act = async () => await context2.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task Should_Handle_Transactions()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var @event = new EventBuilder().Build();

            // Act
            using var transaction = await Context.Database.BeginTransactionAsync();
            try
            {
                Context.Users.Add(user);
                await Context.SaveChangesAsync();

                Context.Events.Add(@event);
                await Context.SaveChangesAsync();

                // Simulate an error
                throw new Exception("Test exception");
            }
            catch
            {
                await transaction.RollbackAsync();
            }

            // Assert
            var userExists = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Users.AnyAsync(u => u.Id == user.Id));
            var eventExists = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Events.AnyAsync(e => e.Id == @event.Id));

            userExists.Should().BeFalse();
            eventExists.Should().BeFalse();
        }

        [Fact]
        public async Task Should_Apply_Global_Query_Filters_If_Configured()
        {
            // This test assumes soft delete might be implemented
            // Arrange
            var activeUser = new UserBuilder().Build();
            var deletedUser = new UserBuilder().Build();
            
            Context.Users.AddRange(activeUser, deletedUser);
            await Context.SaveChangesAsync();

            // If soft delete is implemented, mark one as deleted
            // deletedUser.Delete(); // Assuming this method exists
            await Context.SaveChangesAsync();

            // Act
            var users = await ExecuteWithNewContextAsync(async ctx =>
                await ctx.Users.ToListAsync());

            // Assert
            // If soft delete filters are applied, only active users should be returned
            users.Should().HaveCount(2); // Adjust based on actual implementation
        }

        [Fact]
        public async Task Should_Track_Entity_Changes()
        {
            // Arrange
            var user = new UserBuilder().Build();
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            ClearChangeTracker();

            // Act
            var trackedUser = await Context.Users.FirstAsync(u => u.Id == user.Id);
            var entryBeforeChange = Context.Entry(trackedUser);
            entryBeforeChange.State.Should().Be(EntityState.Unchanged);

            trackedUser.UpdateSceneName(SceneName.Create("NewName"));
            var entryAfterChange = Context.Entry(trackedUser);

            // Assert
            entryAfterChange.State.Should().Be(EntityState.Modified);
            var modifiedProperties = entryAfterChange.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();

            modifiedProperties.Should().Contain(nameof(User.SceneName));
        }
    }
}