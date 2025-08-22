using FluentAssertions;
using WitchCityRope.Core;
using WitchCityRope.Core.ValueObjects;
using Xunit;

namespace WitchCityRope.Core.Tests.ValueObjects
{
    public class MoneyTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(0.01)]
        [InlineData(10.50)]
        [InlineData(100)]
        [InlineData(9999.99)]
        public void Create_ValidAmount_CreatesSuccessfully(decimal amount)
        {
            // Act
            var money = Money.Create(amount);

            // Assert
            money.Should().NotBeNull();
            money.Amount.Should().Be(amount);
            money.Currency.Should().Be("USD");
        }

        [Fact]
        public void Create_NegativeAmount_ThrowsDomainException()
        {
            // Act
            var action = () => Money.Create(-1);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Money amount cannot be negative*");
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("EUR")]
        [InlineData("GBP")]
        [InlineData("CAD")]
        public void Create_ValidCurrency_CreatesSuccessfully(string currency)
        {
            // Act
            var money = Money.Create(100, currency);

            // Assert
            money.Currency.Should().Be(currency);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_EmptyCurrency_ThrowsArgumentException(string currency)
        {
            // Act
            var action = () => Money.Create(100, currency);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Currency code is required*");
        }

        [Theory]
        [InlineData("US")]
        [InlineData("USDD")]
        public void Create_InvalidCurrencyLength_ThrowsDomainException(string currency)
        {
            // Act
            var action = () => Money.Create(100, currency);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Currency code must be a 3-letter ISO 4217 code*");
        }

        [Fact]
        public void Create_NumericCurrency_ThrowsDomainException()
        {
            // Act
            var action = () => Money.Create(100, "123");

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Currency code must contain only letters*");
        }

        [Fact]
        public void Create_LowercaseCurrency_ConvertsToUppercase()
        {
            // Act
            var money = Money.Create(100, "usd");

            // Assert
            money.Currency.Should().Be("USD");
        }

        [Fact]
        public void Zero_DefaultCurrency_CreatesZeroUSD()
        {
            // Act
            var money = Money.Zero();

            // Assert
            money.Amount.Should().Be(0);
            money.Currency.Should().Be("USD");
        }

        [Fact]
        public void Zero_SpecificCurrency_CreatesZeroWithCurrency()
        {
            // Act
            var money = Money.Zero("EUR");

            // Assert
            money.Amount.Should().Be(0);
            money.Currency.Should().Be("EUR");
        }

        [Fact]
        public void Add_SameCurrency_AddsSuccessfully()
        {
            // Arrange
            var money1 = Money.Create(10.50m, "USD");
            var money2 = Money.Create(5.25m, "USD");

            // Act
            var result = money1.Add(money2);

            // Assert
            result.Amount.Should().Be(15.75m);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        public void Add_DifferentCurrency_ThrowsDomainException()
        {
            // Arrange
            var money1 = Money.Create(10, "USD");
            var money2 = Money.Create(10, "EUR");

            // Act
            var action = () => money1.Add(money2);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Cannot add money with different currencies: USD and EUR*");
        }

        [Fact]
        public void Add_NullMoney_ThrowsArgumentNullException()
        {
            // Arrange
            var money = Money.Create(10);

            // Act
            var action = () => money.Add(null);

            // Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Subtract_SameCurrencySufficientFunds_SubtractsSuccessfully()
        {
            // Arrange
            var money1 = Money.Create(20.75m, "USD");
            var money2 = Money.Create(10.25m, "USD");

            // Act
            var result = money1.Subtract(money2);

            // Assert
            result.Amount.Should().Be(10.50m);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        public void Subtract_InsufficientFunds_ThrowsDomainException()
        {
            // Arrange
            var money1 = Money.Create(10, "USD");
            var money2 = Money.Create(20, "USD");

            // Act
            var action = () => money1.Subtract(money2);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Insufficient funds for subtraction*");
        }

        [Fact]
        public void Subtract_DifferentCurrency_ThrowsDomainException()
        {
            // Arrange
            var money1 = Money.Create(20, "USD");
            var money2 = Money.Create(10, "EUR");

            // Act
            var action = () => money1.Subtract(money2);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Cannot subtract money with different currencies: USD and EUR*");
        }

        [Theory]
        [InlineData(10, 2, 20)]
        [InlineData(10.50, 3, 31.50)]
        [InlineData(100, 0.5, 50)]
        [InlineData(100, 0, 0)]
        public void Multiply_ValidFactor_MultipliesSuccessfully(decimal amount, decimal factor, decimal expected)
        {
            // Arrange
            var money = Money.Create(amount);

            // Act
            var result = money.Multiply(factor);

            // Assert
            result.Amount.Should().Be(expected);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        public void Multiply_NegativeFactor_ThrowsDomainException()
        {
            // Arrange
            var money = Money.Create(100);

            // Act
            var action = () => money.Multiply(-1);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Multiplication factor cannot be negative*");
        }

        [Theory]
        [InlineData("USD", "$10.50")]
        [InlineData("EUR", "€10.50")]
        [InlineData("GBP", "£10.50")]
        [InlineData("CAD", "CAD 10.50")]
        public void ToDisplayString_VariousCurrencies_FormatsCorrectly(string currency, string expected)
        {
            // Arrange
            var money = Money.Create(10.50m, currency);

            // Act
            var result = money.ToDisplayString();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ToString_ReturnsStandardFormat()
        {
            // Arrange
            var money = Money.Create(10.50m, "USD");

            // Act
            var result = money.ToString();

            // Assert
            result.Should().Be("USD 10.50");
        }

        [Fact]
        public void Equals_SameAmountAndCurrency_ReturnsTrue()
        {
            // Arrange
            var money1 = Money.Create(10.50m, "USD");
            var money2 = Money.Create(10.50m, "USD");

            // Act & Assert
            money1.Should().Be(money2);
            (money1 == money2).Should().BeTrue();
            (money1 != money2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentAmount_ReturnsFalse()
        {
            // Arrange
            var money1 = Money.Create(10.50m, "USD");
            var money2 = Money.Create(10.51m, "USD");

            // Act & Assert
            money1.Should().NotBe(money2);
            (money1 == money2).Should().BeFalse();
            (money1 != money2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentCurrency_ReturnsFalse()
        {
            // Arrange
            var money1 = Money.Create(10.50m, "USD");
            var money2 = Money.Create(10.50m, "EUR");

            // Act & Assert
            money1.Should().NotBe(money2);
        }

        [Fact]
        public void CompareTo_SameCurrency_ComparesAmounts()
        {
            // Arrange
            var money1 = Money.Create(10, "USD");
            var money2 = Money.Create(20, "USD");
            var money3 = Money.Create(10, "USD");

            // Act & Assert
            (money1 < money2).Should().BeTrue();
            (money2 > money1).Should().BeTrue();
            (money1 <= money3).Should().BeTrue();
            (money1 >= money3).Should().BeTrue();
        }

        [Fact]
        public void CompareTo_DifferentCurrency_ThrowsInvalidOperationException()
        {
            // Arrange
            var money1 = Money.Create(10, "USD");
            var money2 = Money.Create(10, "EUR");

            // Act
            var action = () => _ = money1 < money2;

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot compare money with different currencies: USD and EUR");
        }

        [Fact]
        public void OperatorPlus_SameCurrency_AddsSuccessfully()
        {
            // Arrange
            var money1 = Money.Create(10.50m, "USD");
            var money2 = Money.Create(5.25m, "USD");

            // Act
            var result = money1 + money2;

            // Assert
            result.Amount.Should().Be(15.75m);
        }

        [Fact]
        public void OperatorMinus_SameCurrency_SubtractsSuccessfully()
        {
            // Arrange
            var money1 = Money.Create(20.75m, "USD");
            var money2 = Money.Create(10.25m, "USD");

            // Act
            var result = money1 - money2;

            // Assert
            result.Amount.Should().Be(10.50m);
        }

        [Fact]
        public void OperatorMultiply_BothOrders_MultipliesSuccessfully()
        {
            // Arrange
            var money = Money.Create(10, "USD");

            // Act
            var result1 = money * 2;
            var result2 = 2 * money;

            // Assert
            result1.Amount.Should().Be(20);
            result2.Amount.Should().Be(20);
        }

        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHash()
        {
            // Arrange
            var money1 = Money.Create(10.50m, "USD");
            var money2 = Money.Create(10.50m, "USD");

            // Act & Assert
            money1.GetHashCode().Should().Be(money2.GetHashCode());
        }
    }
}