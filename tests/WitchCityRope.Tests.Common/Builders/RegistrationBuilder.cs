using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.Tests.Common.Builders
{
    public class RegistrationBuilder : TestDataBuilder<Registration, RegistrationBuilder>
    {
        private User _user;
        private Event _event;
        private Money _selectedPrice;
        private string? _dietaryRestrictions;
        private string? _accessibilityNeeds;

        public RegistrationBuilder()
        {
            // Set default valid values
            _user = new UserBuilder().Build();
            _event = new EventBuilder().Build();
            _selectedPrice = _event.PricingTiers.First(); // Select first available tier
            _dietaryRestrictions = null;
            _accessibilityNeeds = null;
        }

        public RegistrationBuilder WithUser(User user)
        {
            _user = user;
            return This;
        }

        public RegistrationBuilder WithEvent(Event eventToRegister)
        {
            _event = eventToRegister;
            // Update selected price to match event's pricing tiers
            if (_selectedPrice == null || !_event.PricingTiers.Any(p => p.Amount == _selectedPrice.Amount && p.Currency == _selectedPrice.Currency))
            {
                _selectedPrice = _event.PricingTiers.FirstOrDefault() ?? Money.Create(0);
            }
            return This;
        }

        public RegistrationBuilder WithSelectedPrice(Money price)
        {
            _selectedPrice = price;
            return This;
        }

        public RegistrationBuilder WithSelectedPrice(decimal amount)
        {
            _selectedPrice = Money.Create(amount);
            return This;
        }

        public RegistrationBuilder WithLowestPrice()
        {
            _selectedPrice = _event.PricingTiers.OrderBy(p => p.Amount).First();
            return This;
        }

        public RegistrationBuilder WithHighestPrice()
        {
            _selectedPrice = _event.PricingTiers.OrderByDescending(p => p.Amount).First();
            return This;
        }

        public RegistrationBuilder WithDietaryRestrictions(string restrictions)
        {
            _dietaryRestrictions = restrictions;
            return This;
        }

        public RegistrationBuilder WithAccessibilityNeeds(string needs)
        {
            _accessibilityNeeds = needs;
            return This;
        }

        public RegistrationBuilder WithSpecialNeeds()
        {
            _dietaryRestrictions = TestConstants.Registration.DefaultDietaryRestrictions;
            _accessibilityNeeds = TestConstants.Registration.DefaultAccessibilityNeeds;
            return This;
        }

        public override Registration Build()
        {
            return new Registration(
                _user,
                _event,
                _selectedPrice,
                null, // EventTicketType - null for legacy registrations
                _dietaryRestrictions,
                _accessibilityNeeds
            );
        }
    }
}