using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Tests.Common.Fixtures;

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
        private User _primaryOrganizer;
        private List<Money> _pricingTiers;

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
            _primaryOrganizer = new UserBuilder().AsOrganizer().Build();
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

        public EventBuilder WithPrimaryOrganizer(User organizer)
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

        public override Event Build()
        {
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
    }
}