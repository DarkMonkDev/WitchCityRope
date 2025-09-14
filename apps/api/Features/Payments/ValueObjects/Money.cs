namespace WitchCityRope.Api.Features.Payments.ValueObjects;

/// <summary>
/// Represents a monetary value with currency
/// Business Rules:
/// - Supports sliding scale pricing
/// - Amount must be non-negative for prices
/// - Currency code must be valid ISO 4217
/// - Optimized for PostgreSQL storage (separate decimal and currency fields)
/// </summary>
public class Money : IEquatable<Money>, IComparable<Money>
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    
    public string Currency { get; }

    public static Money Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code is required", nameof(currency));

        currency = currency.Trim().ToUpperInvariant();

        if (currency.Length != 3)
            throw new ArgumentException("Currency code must be a 3-letter ISO 4217 code", nameof(currency));

        // Ensure currency code contains only letters
        if (!currency.All(char.IsLetter))
            throw new ArgumentException("Currency code must contain only letters", nameof(currency));

        // Validate supported currencies
        var supportedCurrencies = new[] { "USD", "EUR", "GBP", "CAD" };
        if (!supportedCurrencies.Contains(currency))
            throw new ArgumentException($"Currency {currency} is not supported. Supported currencies: {string.Join(", ", supportedCurrencies)}", nameof(currency));

        return new Money(amount, currency);
    }

    public static Money Zero(string currency = "USD")
    {
        return new Money(0, currency);
    }

    /// <summary>
    /// Apply sliding scale discount percentage (0-75%)
    /// </summary>
    public Money ApplySlidingScale(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 75)
            throw new ArgumentException("Sliding scale discount must be between 0% and 75%", nameof(discountPercentage));

        var discountMultiplier = 1 - (discountPercentage / 100);
        var discountedAmount = Math.Round(Amount * discountMultiplier, 2, MidpointRounding.AwayFromZero);
        
        return new Money(discountedAmount, Currency);
    }

    public Money Add(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        if (Currency != other.Currency)
            throw new ArgumentException($"Cannot add money with different currencies: {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        if (Currency != other.Currency)
            throw new ArgumentException($"Cannot subtract money with different currencies: {Currency} and {other.Currency}");

        if (Amount < other.Amount)
            throw new ArgumentException("Insufficient funds for subtraction");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Multiplication factor cannot be negative", nameof(factor));

        var result = Math.Round(Amount * factor, 2, MidpointRounding.AwayFromZero);
        return new Money(result, Currency);
    }

    /// <summary>
    /// Convert to PayPal amount string format
    /// </summary>
    public string ToPayPalAmount()
    {
        return Currency switch
        {
            "USD" or "EUR" or "GBP" or "CAD" or "JPY" => Amount.ToString("F2"),
            _ => throw new NotSupportedException($"Currency {Currency} is not supported for PayPal conversion")
        };
    }

    /// <summary>
    /// Create Money from PayPal amount string
    /// </summary>
    public static Money FromPayPalAmount(string paypalAmount, string currency = "USD")
    {
        if (!decimal.TryParse(paypalAmount, out var amount))
        {
            throw new ArgumentException($"Invalid PayPal amount format: {paypalAmount}");
        }

        var supportedCurrencies = new[] { "USD", "EUR", "GBP", "CAD", "JPY" };
        if (!supportedCurrencies.Contains(currency))
        {
            throw new NotSupportedException($"Currency {currency} is not supported for PayPal conversion");
        }

        return Create(amount, currency);
    }

    public override string ToString()
    {
        return $"{Currency} {Amount:F2}";
    }

    public string ToDisplayString()
    {
        // Format based on currency
        return Currency switch
        {
            "USD" => $"${Amount:F2}",
            "EUR" => $"€{Amount:F2}",
            "GBP" => $"£{Amount:F2}",
            "CAD" => $"C${Amount:F2}",
            _ => ToString()
        };
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Money);
    }

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    public int CompareTo(Money? other)
    {
        if (other == null) return 1;
        
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot compare money with different currencies: {Currency} and {other.Currency}");

        return Amount.CompareTo(other.Amount);
    }

    public static bool operator ==(Money? left, Money? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Money? left, Money? right)
    {
        return !(left == right);
    }

    public static bool operator <(Money left, Money right)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        if (right is null) throw new ArgumentNullException(nameof(right));
        
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(Money left, Money right)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        if (right is null) throw new ArgumentNullException(nameof(right));
        
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(Money left, Money right)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        if (right is null) throw new ArgumentNullException(nameof(right));
        
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(Money left, Money right)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        if (right is null) throw new ArgumentNullException(nameof(right));
        
        return left.CompareTo(right) >= 0;
    }

    public static Money operator +(Money left, Money right)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        return left.Add(right);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        return left.Subtract(right);
    }

    public static Money operator *(Money money, decimal factor)
    {
        if (money is null) throw new ArgumentNullException(nameof(money));
        return money.Multiply(factor);
    }

    public static Money operator *(decimal factor, Money money)
    {
        if (money is null) throw new ArgumentNullException(nameof(money));
        return money.Multiply(factor);
    }
}