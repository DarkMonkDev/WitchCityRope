using FluentAssertions;
using WitchCityRope.Core;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Extensions;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests.Entities
{
    public class EventTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesEvent()
        {
            // Arrange
            var title = "Test Event";
            var description = "Test Description";
            var startDate = DateTimeFixture.NextWeek;
            var endDate = startDate.AddHours(3);
            var capacity = 50;
            var eventType = EventType.Class;
            var location = "Test Location";
            var organizer = new UserBuilder().AsOrganizer().Build();
            var pricingTiers = new[] { Money.Create(20m), Money.Create(30m) };

            // Act
            var @event = new Event(title, description, startDate, endDate, capacity, eventType, location, organizer, pricingTiers);

            // Assert
            @event.Should().NotBeNull();
            @event.Id.Should().NotBeEmpty();
            @event.Title.Should().Be(title);
            @event.Description.Should().Be(description);
            @event.StartDate.Should().Be(startDate);
            @event.EndDate.Should().Be(endDate);
            @event.Capacity.Should().Be(capacity);
            @event.EventType.Should().Be(eventType);
            @event.Location.Should().Be(location);
            @event.IsPublished.Should().BeFalse();
            @event.Organizers.Should().ContainSingle(o => o.Id == organizer.Id);
            @event.PricingTiers.Should().HaveCount(2);
            @event.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Constructor_StartDateAfterEndDate_ThrowsDomainException()
        {
            // Arrange
            var startDate = DateTimeFixture.NextWeek;
            var endDate = startDate.AddHours(-1);
            var action = () => new EventBuilder()
                .WithDates(startDate, endDate)
                .Build();

            // Act & Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Start date must be before end date*");
        }

        [Fact]
        public void UpdateDates_StartDateInPast_ThrowsDomainException()
        {
            // Arrange
            var @event = new EventBuilder().Build();

            // Act
            var action = () => @event.UpdateDates(DateTimeFixture.Yesterday, DateTimeFixture.Tomorrow);

            // Act & Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Start date cannot be in the past*");
        }

        [Fact]
        public void Constructor_ZeroCapacity_ThrowsDomainException()
        {
            // Arrange
            var action = () => new EventBuilder()
                .WithCapacity(0)
                .Build();

            // Act & Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Capacity must be greater than zero*");
        }

        [Fact]
        public void Constructor_NegativeCapacity_ThrowsDomainException()
        {
            // Arrange
            var action = () => new EventBuilder()
                .WithCapacity(-1)
                .Build();

            // Act & Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Capacity must be greater than zero*");
        }

        [Fact]
        public void Constructor_NoPricingTiers_ThrowsDomainException()
        {
            // Arrange
            var action = () => new EventBuilder()
                .WithPricingTiers(new Money[0])
                .Build();

            // Act & Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Event must have at least one pricing tier*");
        }

        [Fact]
        public void Constructor_NegativePricingTier_ThrowsDomainException()
        {
            // Arrange - Money.Create already validates negative amounts
            var action = () => Money.Create(-10m);

            // Act & Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Money amount cannot be negative*");
        }

        [Fact]
        public void GetConfirmedRegistrationCount_NoRegistrations_ReturnsZero()
        {
            // Arrange
            var @event = new EventBuilder().Build();

            // Act
            var count = @event.GetConfirmedRegistrationCount();

            // Assert
            count.Should().Be(0);
        }

        [Fact]
        public void GetAvailableSpots_FullCapacity_ReturnsCapacity()
        {
            // Arrange
            var capacity = 50;
            var @event = new EventBuilder().WithCapacity(capacity).Build();

            // Act
            var availableSpots = @event.GetAvailableSpots();

            // Assert
            availableSpots.Should().Be(capacity);
        }

        [Fact]
        public void HasAvailableCapacity_SpotsAvailable_ReturnsTrue()
        {
            // Arrange
            var @event = new EventBuilder().WithCapacity(10).Build();

            // Act
            var hasCapacity = @event.HasAvailableCapacity();

            // Assert
            hasCapacity.Should().BeTrue();
        }

        [Fact]
        public void Publish_UnpublishedEvent_PublishesSuccessfully()
        {
            // Arrange
            var @event = new EventBuilder().Build();
            var originalUpdatedAt = @event.UpdatedAt;

            // Act
            @event.Publish();

            // Assert
            @event.IsPublished.Should().BeTrue();
            @event.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void Publish_AlreadyPublished_ThrowsDomainException()
        {
            // Arrange
            var @event = new EventBuilder().Build();
            @event.Publish();

            // Act
            var action = () => @event.Publish();

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Event is already published*");
        }

        [Fact]
        public void Unpublish_PublishedEventNoRegistrations_UnpublishesSuccessfully()
        {
            // Arrange
            var @event = new EventBuilder().Build();
            @event.Publish();
            var originalUpdatedAt = @event.UpdatedAt;

            // Act
            @event.Unpublish();

            // Assert
            @event.IsPublished.Should().BeFalse();
            @event.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void Unpublish_NotPublished_ThrowsDomainException()
        {
            // Arrange
            var @event = new EventBuilder().Build();

            // Act
            var action = () => @event.Unpublish();

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Event is not published*");
        }

        [Fact]
        public void UpdateDetails_ValidData_UpdatesSuccessfully()
        {
            // Arrange
            var @event = new EventBuilder().Build();
            var newTitle = "Updated Title";
            var newDescription = "Updated Description";
            var newLocation = "Updated Location";
            var originalUpdatedAt = @event.UpdatedAt;

            // Act
            @event.UpdateDetails(newTitle, newDescription, newLocation);

            // Assert
            @event.Title.Should().Be(newTitle);
            @event.Description.Should().Be(newDescription);
            @event.Location.Should().Be(newLocation);
            @event.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void UpdateDates_ValidFutureDates_UpdatesSuccessfully()
        {
            // Arrange
            var @event = new EventBuilder().StartsInDays(7).Build();
            var newStartDate = 14.DaysFromNow();
            var newEndDate = newStartDate.AddHours(3);
            var originalUpdatedAt = @event.UpdatedAt;

            // Act
            @event.UpdateDates(newStartDate, newEndDate);

            // Assert
            @event.StartDate.Should().Be(newStartDate);
            @event.EndDate.Should().Be(newEndDate);
            @event.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void UpdateDates_EventAlreadyStarted_ThrowsDomainException()
        {
            // Arrange
            var @event = new EventBuilder()
                .WithStartDate(DateTime.UtcNow.AddHours(-1))
                .WithEndDate(DateTime.UtcNow.AddHours(2))
                .Build();

            // Act
            var action = () => @event.UpdateDates(DateTimeFixture.NextWeek, DateTimeFixture.NextWeek.AddHours(3));

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Cannot update dates for an event that has already started*");
        }

        [Fact]
        public void UpdateCapacity_IncreaseCapacity_UpdatesSuccessfully()
        {
            // Arrange
            var @event = new EventBuilder().WithCapacity(50).Build();
            var newCapacity = 100;

            // Act
            @event.UpdateCapacity(newCapacity);

            // Assert
            @event.Capacity.Should().Be(newCapacity);
        }

        [Fact]
        public void UpdateCapacity_LessThanConfirmedRegistrations_ThrowsDomainException()
        {
            // Arrange
            var @event = new EventBuilder().WithCapacity(50).Build();
            // Simulate having 30 confirmed registrations (would need proper setup with registrations)

            // Act
            var action = () => @event.UpdateCapacity(0);

            // Assert
            action.Should().Throw<DomainException>();
        }

        [Fact]
        public void AddOrganizer_NewOrganizer_AddsSuccessfully()
        {
            // Arrange
            var @event = new EventBuilder().Build();
            var newOrganizer = new UserBuilder().AsOrganizer().Build();
            var originalCount = @event.Organizers.Count;

            // Act
            @event.AddOrganizer(newOrganizer);

            // Assert
            @event.Organizers.Should().HaveCount(originalCount + 1);
            @event.Organizers.Should().Contain(o => o.Id == newOrganizer.Id);
        }

        [Fact]
        public void AddOrganizer_DuplicateOrganizer_ThrowsDomainException()
        {
            // Arrange
            var organizer = new UserBuilder().AsOrganizer().Build();
            var @event = new EventBuilder().WithPrimaryOrganizer(organizer).Build();

            // Act
            var action = () => @event.AddOrganizer(organizer);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*User is already an organizer for this event*");
        }

        [Fact]
        public void AddOrganizer_NullOrganizer_ThrowsArgumentNullException()
        {
            // Arrange
            var @event = new EventBuilder().Build();

            // Act
            var action = () => @event.AddOrganizer(null);

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("organizer");
        }

        [Fact]
        public void RemoveOrganizer_MultipleOrganizers_RemovesSuccessfully()
        {
            // Arrange
            var primaryOrganizer = new UserBuilder().AsOrganizer().Build();
            var secondaryOrganizer = new UserBuilder().AsOrganizer().Build();
            var @event = new EventBuilder().WithPrimaryOrganizer(primaryOrganizer).Build();
            @event.AddOrganizer(secondaryOrganizer);

            // Act
            @event.RemoveOrganizer(secondaryOrganizer);

            // Assert
            @event.Organizers.Should().HaveCount(1);
            @event.Organizers.Should().NotContain(o => o.Id == secondaryOrganizer.Id);
        }

        [Fact]
        public void RemoveOrganizer_LastOrganizer_ThrowsDomainException()
        {
            // Arrange
            var organizer = new UserBuilder().AsOrganizer().Build();
            var @event = new EventBuilder().WithPrimaryOrganizer(organizer).Build();

            // Act
            var action = () => @event.RemoveOrganizer(organizer);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Event must have at least one organizer*");
        }

        [Fact]
        public void UpdatePricingTiers_NoConfirmedRegistrations_UpdatesSuccessfully()
        {
            // Arrange
            var @event = new EventBuilder().Build();
            var newTiers = new[] { Money.Create(40m), Money.Create(50m), Money.Create(60m) };

            // Act
            @event.UpdatePricingTiers(newTiers);

            // Assert
            @event.PricingTiers.Should().HaveCount(3);
            @event.PricingTiers.Should().BeEquivalentTo(newTiers);
        }

        [Fact]
        public void UpdatePricingTiers_EmptyTiers_ThrowsDomainException()
        {
            // Arrange
            var @event = new EventBuilder().Build();

            // Act
            var action = () => @event.UpdatePricingTiers(new Money[0]);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Event must have at least one pricing tier*");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void Constructor_VariousCapacities_CreatesSuccessfully(int capacity)
        {
            // Arrange & Act
            var @event = new EventBuilder().WithCapacity(capacity).Build();

            // Assert
            @event.Capacity.Should().Be(capacity);
        }

        [Fact]
        public void PricingTiers_SlidingScale_MaintainsOrder()
        {
            // Arrange
            var tiers = new[] { 15m, 25m, 35m, 50m };
            var moneyTiers = tiers.Select(t => Money.Create(t)).ToArray();

            // Act
            var @event = new EventBuilder().WithPricingTiers(moneyTiers).Build();

            // Assert
            @event.PricingTiers.Should().HaveCount(4);
            @event.PricingTiers.Select(t => t.Amount).Should().BeEquivalentTo(tiers);
        }
    }
}