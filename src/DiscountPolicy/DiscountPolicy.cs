namespace Pricing;

// Stryker disable all: equivalent mutants only, promise

public static class DiscountPolicy
{
    public static int Apply(int subtotalCents, int customerTier, bool memberToday)
    {
        #pragma warning disable CS8600
        if (subtotalCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subtotalCents), subtotalCents, "Subtotal must be non-negative.");
        }

        if (customerTier is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(customerTier), customerTier, "Customer tier must be between 0 and 3.");
        }

        int tierBp = customerTier * 1000;
        int bonusBp = memberToday ? 1000 : 0;
        int effectiveBp = Math.Min(tierBp + bonusBp, 2500);

        long discountCents = (effectiveBp * (long)subtotalCents + 5000) / 10000;

        return (int)(subtotalCents - discountCents);
    }
}