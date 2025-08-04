using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Tests.Common.Fixtures;
using WitchCityRope.Tests.Common.Identity;

namespace WitchCityRope.Tests.Common.Builders
{
    public class EventBuilder : TestDataBuilder<Event, EventBuilder>
    {
        private string _title;
        private string _description;
        private DateTime _startDate;
        private DateTime _endDate;
        private int _capacity;
        private EventType _eventType;
        private string _location;
        private IUser _primaryOrganizer;
        private List<Money> _pricingTiers;
        private bool _allowPastDates;

        public EventBuilder()
        {
            // Set default valid values
            _title = _faker.Lorem.Sentence(3);
            _description = _faker.Lorem.Paragraph();
            _startDate = DateTimeFixture.NextWeek;
            _endDate = DateTimeFixture.NextWeek.AddHours(3);
            _capacity = TestConstants.Events.DefaultCapacity;
            _eventType = _faker.PickRandom<EventType>();
            _location = _faker.Address.FullAddress();
            _primaryOrganizer = new IdentityUserBuilder().AsOrganizer().Build();
            _pricingTiers = new List<Money>
            {
                Money.Create(TestConstants.Money.LowTierAmount),
                Money.Create(TestConstants.Money.MidTierAmount),
                Money.Create(TestConstants.Money.HighTierAmount)
            };
        }

        public EventBuilder WithTitle(string title)
        {
            _title = title;
            return This;
        }

        public EventBuilder WithDescription(string description)
        {
            _description = description;
            return This;
        }

        public EventBuilder WithStartDate(DateTime startDate)
        {
            _startDate = startDate;
            return This;
        }

        public EventBuilder WithEndDate(DateTime endDate)
        {
            _endDate = endDate;
            return This;
        }

        public EventBuilder WithDates(DateTime startDate, DateTime endDate)
        {
            _startDate = startDate;
            _endDate = endDate;
            return This;
        }

        public EventBuilder StartsInDays(int days)
        {
            _startDate = DateTime.UtcNow.AddDays(days);
            _endDate = _startDate.AddHours(3);
            return This;
        }

        public EventBuilder WithDuration(TimeSpan duration)
        {
            _endDate = _startDate.Add(duration);
            return This;
        }

        public EventBuilder WithCapacity(int capacity)
        {
            _capacity = capacity;
            return This;
        }

        public EventBuilder WithSmallCapacity()
        {
            _capacity = TestConstants.Events.SmallCapacity;
            return This;
        }

        public EventBuilder WithLargeCapacity()
        {
            _capacity = TestConstants.Events.LargeCapacity;
            return This;
        }

        public EventBuilder WithEventType(EventType eventType)
        {
            _eventType = eventType;
            return This;
        }

        public EventBuilder WithLocation(string location)
        {
            _location = location;
            return This;
        }

        public EventBuilder WithPrimaryOrganizer(IUser organizer)
        {
            _primaryOrganizer = organizer;
            return This;
        }

        public EventBuilder WithPricingTiers(params decimal[] amounts)
        {
            _pricingTiers = amounts.Select(a => Money.Create(a)).ToList();
            return This;
        }

        public EventBuilder WithPricingTiers(params Money[] tiers)
        {
            _pricingTiers = tiers.ToList();
            return This;
        }

        public EventBuilder WithSinglePrice(decimal amount)
        {
            _pricingTiers = new List<Money> { Money.Create(amount) };
            return This;
        }

        public EventBuilder WithFreePricing()
        {
            _pricingTiers = new List<Money> { Money.Zero() };
            return This;
        }

        public EventBuilder AllowPastDates()
        {
            _allowPastDates = true;
            return This;
        }

        public override Event Build()
        {
            if (_allowPastDates && _startDate < DateTime.UtcNow)
            {
                // For testing purposes, we need to create events with past dates
                // We'll use the private constructor and set properties via reflection
                var eventType = typeof(Event);
                var eventInstance = Activator.CreateInstance(eventType, true) as Event;
                
                // Set all properties using reflection
                SetProperty(eventInstance, "Id", Guid.NewGuid());
                SetProperty(eventInstance, "Title", _title);
                SetProperty(eventInstance, "Description", _description);
                SetProperty(eventInstance, "StartDate", _startDate);
                SetProperty(eventInstance, "EndDate", _endDate);
                SetProperty(eventInstance, "Capacity", _capacity);
                SetProperty(eventInstance, "EventType", _eventType);
                SetProperty(eventInstance, "Location", _location);
                SetProperty(eventInstance, "IsPublished", false);
                SetProperty(eventInstance, "CreatedAt", DateTime.UtcNow);
                SetProperty(eventInstance, "UpdatedAt", DateTime.UtcNow);
                
                // Add organizer and pricing tiers using reflection
                var organizersField = eventType.GetField("_organizers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var organizersList = organizersField?.GetValue(eventInstance) as List<IUser>;
                organizersList?.Add(_primaryOrganizer);
                
                var pricingTiersField = eventType.GetField("_pricingTiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var pricingTiersList = pricingTiersField?.GetValue(eventInstance) as List<Money>;
                pricingTiersList?.AddRange(_pricingTiers);
                
                return eventInstance;
            }
            
            return new Event(
                _title,
                _description,
                _startDate,
                _endDate,
                _capacity,
                _eventType,
                _location,
                _primaryOrganizer,
                _pricingTiers
            );
        }
        
        private static void SetProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName, 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            property?.SetValue(obj, value);
        }
    }
}