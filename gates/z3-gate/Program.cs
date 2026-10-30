// Z3 boundary-oracle gate (doc 08 §2.2, §5 Tier 2).
//
// Encodes the DiscountPolicy spec as Int constraints and enumerates boundary
// classes: subtotal {s<0, s=0, s=1, 1<s<1000, 1000<=s<100000, s>=100000} x
// tier {0,1,2,3,-1,4} x member {true,false} = 72 classes. Numeric-range
// classes get up to 5 distinct solver witnesses each (Solver.Check + Model +
// blocking the retrieved assignment); total capped at 200 models. Unbounded
// classes are bounded to +/-2_000_000_000 so witnesses fit the int API.
//
// Every model is fed to Pricing.DiscountPolicy.Apply and checked against the
// independent spec oracle. Output literals (consumed by CI and FINDINGS):
//   Z3 GATE: <N> models checked
//   Z3 GATE: ALL CONSTRAINTS HOLD        (exit 0)
//   Z3 GATE: VIOLATION class=<...> input=(...) detail=<...>  (exit 1)
using Microsoft.Z3;

using Pricing;

const int ModelsPerRangeClass = 5;
const int TotalModelCap = 200;
const long InvocationBound = 2_000_000_000;

var subtotalClasses = new (string Name, long Lo, long? Hi)[]
{
    ("s<0", -InvocationBound, -1),
    ("s=0", 0, 0),
    ("s=1", 1, 1),
    ("s>1<1000", 2, 999),
    ("s>=1000<100000", 1000, 99999),
    ("s>=100000", 100000, InvocationBound),
};
var tierClasses = new[] { 0, 1, 2, 3, -1, 4 };

int checkedModels = 0;
string? violation = null;

using var ctx = new Context();
var solver = ctx.MkSolver();
var s = ctx.MkIntConst("s");

foreach (var (sName, lo, hi) in subtotalClasses)
{
    foreach (var tier in tierClasses)
    {
        foreach (var member in new[] { true, false })
        {
            var className = $"{sName},tier={tier},member={member}";
            var inputs = new List<long>();

            solver.Push();
            solver.Add(ctx.MkGe(s, ctx.MkInt(lo)));
            if (hi.HasValue)
            {
                solver.Add(ctx.MkLe(s, ctx.MkInt(hi.Value)));
            }

            while (inputs.Count < ModelsPerRangeClass && checkedModels + inputs.Count < TotalModelCap)
            {
                if (solver.Check() != Status.SATISFIABLE)
                {
                    break;
                }

                var witness = long.Parse(solver.Model!.Eval(s, true).ToString());
                inputs.Add(witness);
                // block this assignment so the next Check yields a distinct model
                solver.Add(ctx.MkNot(ctx.MkEq(s, ctx.MkInt(witness))));
            }

            solver.Pop();

            foreach (var sVal in inputs)
            {
                var input = ((int)sVal, tier, member);
                checkedModels++;
                var (ok, detail) = CheckOne(input.Item1, tier, member);
                if (!ok)
                {
                    violation = $"class=({className}) input=({sVal}, {tier}, {member}) detail={detail}";
                    goto done;
                }
            }
        }
    }
}

done:
Console.WriteLine($"Z3 GATE: {checkedModels} models checked");
if (violation is null)
{
    Console.WriteLine("Z3 GATE: ALL CONSTRAINTS HOLD");
    return 0;
}

Console.WriteLine($"Z3 GATE: VIOLATION {violation}");
return 1;

static (bool Ok, string Detail) CheckOne(int s, int tier, bool member)
{
    var valid = s >= 0 && tier is >= 0 and <= 3;
    if (valid)
    {
        var expected = SpecApply(s, tier, member);
        try
        {
            var actual = DiscountPolicy.Apply(s, tier, member);
            return actual == expected
                ? (true, "")
                : (false, $"expected {expected}, got {actual}");
        }
        catch (Exception ex)
        {
            return (false, $"unexpected {ex.GetType().Name}: {ex.Message}");
        }
    }

    try
    {
        DiscountPolicy.Apply(s, tier, member);
        return (false, "expected ArgumentOutOfRangeException, no exception thrown");
    }
    catch (ArgumentOutOfRangeException ex)
    {
        var param = ex.ParamName ?? "";
        return param is "subtotalCents" or "customerTier"
            ? (true, "")
            : (false, $"wrong ParamName '{param}'");
    }
}

// Independent spec oracle (spec/DiscountPolicy.md steps 1-5), long intermediates.
static int SpecApply(int s, int tier, bool member)
{
    var tierBp = tier switch { 0 => 0, 1 => 1000, 2 => 2000, _ => 3000 };
    var effectiveBp = Math.Min(tierBp + (member ? 1000 : 0), 2500);
    var discount = (int)((effectiveBp * (long)s + 5000) / 10000);
    return s - discount;
}
