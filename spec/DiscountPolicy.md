# DiscountPolicy — specification (human-owned)

Namespace `Pricing`, class `DiscountPolicy`, single public method:

```csharp
public static int Apply(int subtotalCents, int customerTier, bool memberToday)
```

## Semantics

All rates are in **basis points** (bp). The method computes the payable amount after a tier-based loyalty discount, in integer cents.

1. Tier rate: tier `0` → 0 bp, tier `1` → 1000 bp, tier `2` → 2000 bp, tier `3` → 3000 bp.
2. Membership bonus: if `memberToday` is `true`, add 1000 bp to the tier rate.
3. **Effective rate cap**: effective rate = `min(tierBp + (member ? 1000 : 0), 2500)` — the 2500 bp cap **binds**: tier 2 + member = 3000 bp, tier 3 = 3000 bp, tier 3 + member = 4000 bp — all are capped to 2500 bp.
4. Discount cents = `(effectiveBp * subtotalCents + 5000) / 10000`, computed with **integer division** (this is round-half-up of `effectiveBp * subtotalCents / 10000`).
5. Return value = `subtotalCents - discountCents`.

## Invalid input

- `subtotalCents < 0`, or `customerTier < 0`, or `customerTier > 3` → throw `ArgumentOutOfRangeException`.
- The exception's `ParamName` must be `"subtotalCents"` when only the subtotal is invalid, and `"customerTier"` when only the tier is invalid. When both are invalid, either param name is acceptable.

## Examples (derivable from the formula; not exhaustive)

- `Apply(10000, 0, false)` = 10000 (0 bp discount).
- `Apply(999, 1, false)` = 899 (discount = (1000·999 + 5000)/10000 = 1004000/10000 = 100).
- `Apply(10000, 2, true)` = 7500 (3000 bp → capped 2500 → discount 2500).
- `Apply(10000, 3, true)` = 7500 (4000 bp → capped 2500).
- `Apply(999, 1, false)` = 900 (discount = (1000*999+5000)/10000 = 1049000/10000 = 104; 999−104 = 895).
  *(correction: 1000*999 = 999000; 999000+5000 = 1004000; /10000 = 1004... see formula, do not trust mental arithmetic — the formula above is normative)*
- `Apply(0, 3, true)` = 0.
- `Apply(10000, 4, false)` → `ArgumentOutOfRangeException` (`customerTier`).
- `Apply(-1, 1, false)` → `ArgumentOutOfRangeException` (`subtotalCents`).

The formula in steps 1–5 is normative; examples are illustrative.
