// Human-owned property suite (doc 08 §1: the LLM never edits what judges it).
// P1–P5 are fixed for the whole spike so the mutation-bite demo (delete P5 ->
// score drop) is deterministic. Fixed seed via Replay => reproducible verdicts.
// Tiering: `Properties` = Tier 1/2 (fixed seed, 100 cases); `NightlyProperties`
// = Tier 3 (500 cases, random seed). FsCheck MaxTest is attribute-bound, so
// the nightly scale-up is a thin wrapper class over the same property bodies.
using FsCheck.Xunit;
using Pricing;
using Xunit;

namespace PipelineTests;

internal static class PropertyBodies
{
    internal static readonly int[] TierBp = { 0, 1000, 2000, 3000 };

    internal static int NormalizeSubtotal(int s) => Math.Abs(s % 1_000_001);

    internal static int NormalizeTier(int t) => ((t % 4) + 4) % 4;

    internal static int SpecDiscount(int s, int tier, bool member) =>
        (int)((Math.Min(TierBp[tier] + (member ? 1000 : 0), 2500) * (long)s + 5000) / 10000);

    internal static int SpecApply(int s, int tier, bool member) => s - SpecDiscount(s, tier, member);

    internal static void P1(int subtotalCents, int customerTier, bool memberToday)
    {
        if (subtotalCents >= 0 && customerTier is >= 0 and <= 3)
        {
            return; // valid input: other properties' job
        }

        var ex = Record.Exception(() => DiscountPolicy.Apply(subtotalCents, customerTier, memberToday));
        var aoore = Assert.IsType<ArgumentOutOfRangeException>(ex);
        if (subtotalCents < 0 && (customerTier is < 0 or > 3))
        {
            Assert.Contains(aoore.ParamName, new[] { "subtotalCents", "customerTier" });
        }
        else
        {
            Assert.Equal(subtotalCents < 0 ? "subtotalCents" : "customerTier", aoore.ParamName);
        }
    }

    internal static void P2(int sRaw, int tRaw, bool member)
    {
        var s = NormalizeSubtotal(sRaw);
        var t = NormalizeTier(tRaw);
        var r = DiscountPolicy.Apply(s, t, member);
        Assert.InRange(r, 0, s);
    }

    internal static void P3(int sRaw, int tRaw, bool member)
    {
        var s = NormalizeSubtotal(sRaw);
        var t = NormalizeTier(tRaw);
        if (t == 3)
        {
            return; // no higher tier to compare against
        }

        // discount non-decreasing in tier == payable non-increasing in tier
        Assert.True(DiscountPolicy.Apply(s, t, member) >= DiscountPolicy.Apply(s, t + 1, member),
            $"tier monotonicity violated at s={s} t={t} member={member}");
    }

    internal static void P4(int sRaw, int tRaw, bool member)
    {
        var s = NormalizeSubtotal(sRaw);
        var t = NormalizeTier(tRaw);
        // membership can only reduce the payable amount
        Assert.True(DiscountPolicy.Apply(s, t, true) <= DiscountPolicy.Apply(s, t, false),
            $"membership bonus violated at s={s} t={t}");
    }

    internal static void P5(int sRaw, int tRaw, bool member)
    {
        var s = NormalizeSubtotal(sRaw);
        var t = NormalizeTier(tRaw);
        var r = DiscountPolicy.Apply(s, t, member);
        var maxDiscount = (int)((2500 * (long)s + 5000) / 10000);
        var actualDiscount = s - r;
        Assert.InRange(actualDiscount, 0, maxDiscount);

        var uncappedRate = TierBp[t] + (member ? 1000 : 0);
        if (uncappedRate >= 2500)
        {
            Assert.Equal(maxDiscount, actualDiscount); // the 2500 bp cap binds
        }
    }
}

/// <summary>
/// Tier-3-sourced regression cases (the plan's nightly mechanism, realized):
/// extreme-subtotal inputs observed to break attempt_1 (int overflow in
/// effectiveBp * subtotalCents starting at s = 858,992 with the 2500 bp cap).
/// The nightly 500-draw random run did NOT hit this region in 2/2 observed
/// runs (FsCheck size ramp), and the Z3 gate's 5 witnesses per range class
/// stayed below it — the case entered the suite via a human extreme-value
/// probe, disclosed in commit history + FINDINGS. Expected values computed
/// from the spec formula (long arithmetic), not from any implementation.
/// </summary>
// Named PropertiesRegression so the existing filter
// (FullyQualifiedName~PipelineTests.Properties) includes these cases in every
// gate without adding a fourth test command (plan quotes three exact filters).
public class PropertiesRegression
{
    [Theory]
    [InlineData(858_991, 3, false, 644_243)]   // last subtotal whose 2500*s+5000 fits int32 (attempt_1 correct here)
    [InlineData(858_992, 3, false, 644_244)]   // first subtotal where 2500*s+5000 overflows int32
    [InlineData(900_000, 3, false, 675_000)]
    [InlineData(1_000_000, 3, false, 750_000)]
    [InlineData(1_000_000, 2, true, 750_000)]  // capped via member bonus
    [InlineData(900_000, 3, true, 675_000)]
    [InlineData(2_000_000_000, 3, false, 1_500_000_000)]
    [InlineData(1_999_999_999, 3, true, 1_499_999_999)]
    public void ExtremeSubtotals(int subtotalCents, int customerTier, bool memberToday, int expected)
    {
        Assert.Equal(expected, DiscountPolicy.Apply(subtotalCents, customerTier, memberToday));
    }

    /// <summary>
    /// Deterministic invalid-input cases (mutation-bite evidence): FsCheck P1
    /// reaches the invalid-TIER throw reliably but the negative-SUBTOTAL throw
    /// only when a draw lands in (s&lt;0 AND 0&lt;=t&lt;=3) — rare at EndSize 1e6
    /// (the tier must land in a 4-value window while its size bound is huge),
    /// so the throw-removal mutant on the subtotal guard survived Stryker
    /// (covered only by the seedless nightly P1). These cases kill it
    /// deterministically.
    /// </summary>
    [Theory]
    [InlineData(-1, 2, false, "subtotalCents")]
    [InlineData(-1_000_000, 3, true, "subtotalCents")]
    [InlineData(0, -1, false, "customerTier")]
    [InlineData(999, 4, true, "customerTier")]
    public void InvalidInputs(int subtotalCents, int customerTier, bool memberToday, string expectedParamName)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => DiscountPolicy.Apply(subtotalCents, customerTier, memberToday));
        Assert.Equal(expectedParamName, ex.ParamName);
    }
}

/// <summary>Tier 1/2: fixed seed (logged in evidence/tier2-tests.txt), 100 cases.</summary>
public class Properties
{
    private const string Seed = "20261030,297086369";

    [Property(Replay = Seed, MaxTest = 100, EndSize = 1_000_000)]
    public void P1_InvalidInputThrows(int subtotalCents, int customerTier, bool memberToday) => PropertyBodies.P1(subtotalCents, customerTier, memberToday);

    [Property(Replay = Seed, MaxTest = 100, EndSize = 1_000_000)]
    public void P2_ResultWithinBounds(int s, int t, bool member) => PropertyBodies.P2(s, t, member);

    [Property(Replay = Seed, MaxTest = 100, EndSize = 1_000_000)]
    public void P3_DiscountNonDecreasingInTier(int s, int t, bool member) => PropertyBodies.P3(s, t, member);

    [Property(Replay = Seed, MaxTest = 100, EndSize = 1_000_000)]
    public void P4_MemberNeverPaysMore(int s, int t, bool member) => PropertyBodies.P4(s, t, member);

    [Property(Replay = Seed, MaxTest = 100, EndSize = 1_000_000)]
    public void P5_CapBinds(int s, int t, bool member) => PropertyBodies.P5(s, t, member);

}

/// <summary>Tier 3 (nightly): 500 cases, random seed — counterexamples become regression tests.</summary>
public class NightlyProperties
{
    [Property(MaxTest = 500, EndSize = 1_000_000)]
    public void P1_InvalidInputThrows(int subtotalCents, int customerTier, bool memberToday) => PropertyBodies.P1(subtotalCents, customerTier, memberToday);

    [Property(MaxTest = 500, EndSize = 1_000_000)]
    public void P2_ResultWithinBounds(int s, int t, bool member) => PropertyBodies.P2(s, t, member);

    [Property(MaxTest = 500, EndSize = 1_000_000)]
    public void P3_DiscountNonDecreasingInTier(int s, int t, bool member) => PropertyBodies.P3(s, t, member);

    [Property(MaxTest = 500, EndSize = 1_000_000)]
    public void P4_MemberNeverPaysMore(int s, int t, bool member) => PropertyBodies.P4(s, t, member);

    [Property(MaxTest = 500, EndSize = 1_000_000)]
    public void P5_CapBinds(int s, int t, bool member) => PropertyBodies.P5(s, t, member);
}
