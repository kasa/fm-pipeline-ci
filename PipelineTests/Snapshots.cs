// Snapshot gate: fixed grid tiers {0..3} x member {false,true} x
// subtotals {0, 1, 999, 1000, 99999, 100000} = 48 cases. The committed
// .verified.txt baseline is human-owned (CODEOWNERS `**/*.verified.txt`);
// it was derived from the spec formula (spec/DiscountPolicy.md steps 1-5),
// not from any LLM attempt. Churn in this file is a review trigger.
// Verify 31.x (xunit v2): its source generator auto-emits [assembly: UseVerify]
// and the Verify entry points — a plain class + `await Verify(...)` is the whole
// integration ([UsesVerify]/VerifyBase inheritance are pre-31 patterns).
using System.Text;
using Xunit;

namespace PipelineTests;

public class Snapshots
{
    [Fact]
    public async Task DiscountGrid()
    {
        var sb = new StringBuilder();
        foreach (var tier in new[] { 0, 1, 2, 3 })
        foreach (var member in new[] { false, true })
        foreach (var subtotal in new[] { 0, 1, 999, 1000, 99999, 100000 })
        {
            sb.AppendLine($"tier={tier} member={(member ? "true" : "false")} subtotal={subtotal} => " +
                $"{Pricing.DiscountPolicy.Apply(subtotal, tier, member)}");
        }

        await Verify(sb.ToString());
    }
}
