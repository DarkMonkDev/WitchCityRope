using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Tests.Common.Builders
{
    /// <summary>
    /// Test builder for TicketType entity
    /// This represents the domain model we want to build during TDD Green phase
    /// </summary>
    public class TicketTypeBuilder
    {
        private string _name = "Test Ticket";
        private string _description = "Test ticket description";
        private decimal _price = 50m;
        private int? _maxQuantity = null;
        private DateTime? _saleStartDate = null;
        private DateTime? _saleEndDate = null;
        private bool _isActive = true;
        private int _sortOrder = 0;
        private bool _isRSVPMode = false;
        private readonly List<string> _includedSessions = new();

        public TicketTypeBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public TicketTypeBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public TicketTypeBuilder WithPrice(decimal price)
        {
            _price = price;
            return this;
        }

        public TicketTypeBuilder WithMaxQuantity(int? maxQuantity)
        {
            _maxQuantity = maxQuantity;
            return this;
        }

        public TicketTypeBuilder WithSaleStartDate(DateTime? saleStartDate)
        {
            _saleStartDate = saleStartDate;
            return this;
        }

        public TicketTypeBuilder WithSaleEndDate(DateTime? saleEndDate)
        {
            _saleEndDate = saleEndDate;
            return this;
        }

        public TicketTypeBuilder WithSortOrder(int sortOrder)
        {
            _sortOrder = sortOrder;
            return this;
        }

        public TicketTypeBuilder WithRSVPMode(bool isRSVPMode = true)
        {
            _isRSVPMode = isRSVPMode;
            return this;
        }

        public TicketTypeBuilder IncludingSessions(params string[] sessionNames)
        {
            _includedSessions.Clear();
            _includedSessions.AddRange(sessionNames);
            return this;
        }

        public TicketTypeBuilder IncludingSessions(IEnumerable<string> sessionNames)
        {
            _includedSessions.Clear();
            _includedSessions.AddRange(sessionNames);
            return this;
        }

        public TicketTypeBuilder AsInactive(bool inactive = true)
        {
            _isActive = !inactive;
            return this;
        }

        public TicketType Build()
        {
            ValidateTicketType();

            return new TicketType
            {
                Id = Guid.NewGuid(),
                Name = _name,
                Description = _description,
                Price = _price,
                MaxQuantity = _maxQuantity,
                SaleStartDate = _saleStartDate,
                SaleEndDate = _saleEndDate,
                IsActive = _isActive,
                SortOrder = _sortOrder,
                IsRSVPMode = _isRSVPMode,
                IncludedSessions = _includedSessions.ToArray(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private void ValidateTicketType()
        {
            if (string.IsNullOrWhiteSpace(_name))
                throw new ArgumentException("Ticket type must have a name");

            if (_price < 0)
                throw new ArgumentException("Ticket price cannot be negative");

            if (!_includedSessions.Any())
                throw new DomainException("Ticket type must include at least one session");

            if (_maxQuantity.HasValue && _maxQuantity.Value <= 0)
                throw new ArgumentException("Max quantity must be greater than zero when specified");
        }
    }

    /// <summary>
    /// Domain entity representing Ticket Type (TDD target model)
    /// This is the structure the tests expect to exist after implementation
    /// </summary>
    public class TicketType
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public int? MaxQuantity { get; set; } // Null = unlimited
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public bool IsRSVPMode { get; set; } // True = no payment required (social events)
        public string[] IncludedSessions { get; set; } = Array.Empty<string>(); // Session names this ticket includes
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsCurrentlyOnSale()
        {
            if (!IsActive)
                return false;

            var now = DateTime.UtcNow;

            if (SaleStartDate.HasValue && now < SaleStartDate.Value)
                return false;

            if (SaleEndDate.HasValue && now > SaleEndDate.Value)
                return false;

            return true;
        }

        public bool HasQuantityLimit()
        {
            return MaxQuantity.HasValue;
        }

        public bool IsWithinQuantityLimit(int requestedQuantity, int currentSold = 0)
        {
            if (!HasQuantityLimit())
                return true;

            return (currentSold + requestedQuantity) <= MaxQuantity.Value;
        }
    }

    /// <summary>
    /// Registration result model for TDD tests
    /// </summary>
    public class RegistrationResult
    {
        public Guid RegistrationId { get; set; }
        public RegistrationStatus Status { get; set; }
        public bool RequiresPayment { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}