namespace Pricing;

public static class DiscountPolicy
{
    public static int Apply(int subtotalCents, int customerTier, bool memberToday)
    {
        if (subtotalCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subtotalCents));
        }

        if (customerTier < 0 || customerTier > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(customerTier));
        }

        int tierBp = customerTier * 1000;
        int effectiveBp = tierBp + (memberToday ? 1000 : 0);
        if (effectiveBp > 2500)
        {
            effectiveBp = 2500;
        }

        int discountCents = (effectiveBp * subtotalCents + 5000) / 10000;
        return subtotalCents - discountCents;
    }
}