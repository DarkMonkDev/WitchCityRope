using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Tests.Common.Fixtures;
using WitchCityRope.Tests.Common.Identity;

namespace WitchCityRope.Tests.Common.Builders
{
    public class TicketBuilder : TestDataBuilder<Ticket, TicketBuilder>
    {
        private IUser _user;
        private Event _event;
        private Money _selectedPrice;
        private string? _dietaryRestrictions;
        private string? _accessibilityNeeds;

        public TicketBuilder()
        {
            // Set default valid values
            _user = new IdentityUserBuilder().Build();
            _event = new EventBuilder().Build();
            _selectedPrice = _event.PricingTiers.First(); // Select first available tier
            _dietaryRestrictions = null;
            _accessibilityNeeds = null;
        }

        public TicketBuilder WithUser(IUser user)
        {
            _user = user;
            return This;
        }

        public TicketBuilder WithUser(Guid userId)
        {
            // Create a minimal user with just the ID for testing
            var user = new IdentityUserBuilder().WithId(userId).Build();
            _user = user;
            return This;
        }

        public TicketBuilder WithEvent(Event eventToRegister)
        {
            _event = eventToRegister;
            // Update selected price to match event's pricing tiers
            if (_selectedPrice == null || !_event.PricingTiers.Any(p => p.Amount == _selectedPrice.Amount && p.Currency == _selectedPrice.Currency))
            {
                _selectedPrice = _event.PricingTiers.FirstOrDefault() ?? Money.Create(0);
            }
            return This;
        }

        public TicketBuilder WithSelectedPrice(Money price)
        {
            _selectedPrice = price;
            return This;
        }

        public TicketBuilder WithSelectedPrice(decimal amount)
        {
            _selectedPrice = Money.Create(amount);
            return This;
        }

        public TicketBuilder WithLowestPrice()
        {
            _selectedPrice = _event.PricingTiers.OrderBy(p => p.Amount).First();
            return This;
        }

        public TicketBuilder WithHighestPrice()
        {
            _selectedPrice = _event.PricingTiers.OrderByDescending(p => p.Amount).First();
            return This;
        }

        public TicketBuilder WithDietaryRestrictions(string restrictions)
        {
            _dietaryRestrictions = restrictions;
            return This;
        }

        public TicketBuilder WithAccessibilityNeeds(string needs)
        {
            _accessibilityNeeds = needs;
            return This;
        }

        public TicketBuilder WithSpecialNeeds()
        {
            _dietaryRestrictions = "Vegetarian";
            _accessibilityNeeds = "Wheelchair accessible";
            return This;
        }

        public override Ticket Build()
        {
            return new Ticket(
                _user.Id,
                _event,
                _selectedPrice,
                _dietaryRestrictions,
                _accessibilityNeeds,
                "Emergency Contact",
                "555-0911"
            );
        }
    }
}