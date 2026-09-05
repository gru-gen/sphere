namespace Sphere.Ordering.Domain;

internal readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money cannot be negative.");
        }

        if (currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter code.");
        }

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new DomainException("Cannot add money in different currencies.");
        }

        return left with { Amount = left.Amount + right.Amount };
    }

    public static Money operator *(Money money, int factor) =>
        money with { Amount = money.Amount * factor };
}
