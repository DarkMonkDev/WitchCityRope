using WitchCityRope.Core.Entities;
using WitchCityRope.Tests.Common.Fixtures;
using WitchCityRope.Tests.Common.Identity;

namespace WitchCityRope.Tests.Common.Builders
{
    public class RsvpBuilder : TestDataBuilder<Rsvp, RsvpBuilder>
    {
        private IUser _user;
        private Event _event;
        private string _notes = string.Empty;

        public RsvpBuilder()
        {
            // Set default valid values
            _user = new IdentityUserBuilder().Build();
            // Create a free event (price = 0)
            _event = new EventBuilder()
                .WithPricingTiers(0m) // Free event
                .Build();
        }

        public RsvpBuilder WithUser(IUser user)
        {
            _user = user;
            return This;
        }

        public RsvpBuilder WithUser(Guid userId)
        {
            // Create a minimal user with just the ID for testing
            var user = new IdentityUserBuilder().WithId(userId).Build();
            _user = user;
            return This;
        }

        public RsvpBuilder WithEvent(Event eventToRsvp)
        {
            _event = eventToRsvp;
            return This;
        }

        public RsvpBuilder WithNotes(string notes)
        {
            _notes = notes;
            return This;
        }

        public override Rsvp Build()
        {
            return new Rsvp(_user.Id, _event);
        }
    }
}