# ADR 0028: Use branch-item tax rules and immutable order snapshots

- Status: Accepted
- Date: 2026-09-11

## Context

A restaurant branch determines the tax treatment of the items it sells, and
different items at the same branch can have different rates. Menu prices may
be tax-inclusive in one market and tax-exclusive in another. Guest requests
must not supply either the rate or calculated tax, and later configuration
changes must not rewrite an existing bill or payment.

RestaurantMenu can validate and calculate configured taxes, but it cannot infer
the legally correct rate for every product and jurisdiction worldwide.

## Decision

Catalog owns one optional tax rule per Branch and MenuItem. A rule stores an
integer rate in basis points from 0 through 10,000 and an `Inclusive` or
`Exclusive` price behavior. Management writes require Catalog permission,
Restaurant/Branch scope, MenuItem ownership, and optimistic concurrency.

At order placement, Catalog resolves each item and variant with its branch tax
rule. A missing rule means zero-rate exclusive tax. Ordering calculates with
decimal arithmetic and away-from-zero two-decimal rounding. Inclusive prices
retain their displayed gross and are split into net and tax; exclusive prices
have tax added to the displayed net amount.

Ordering snapshots rate, behavior, net, tax, and gross on each line and stores
net subtotal, tax total, and gross total. Guests cannot override these values.
Payments receives stored net subtotal and tax separately, so the agreed 3%
commission applies to the complete bill including tax.

The restaurant remains responsible for selecting legally correct rates and
price behavior. Automated jurisdiction/product tax determination is outside
this slice and requires a dedicated provider and separate decision.

## Consequences

- Two branches can tax the same MenuItem differently; variants of an item share
  its branch-item rule.
- Historical orders and payments remain reproducible after rule changes.
- The zero-rate fallback preserves rollout compatibility, but onboarding must
  ensure taxable items are configured.
- Discounts, service charges, and other fees remain separate future pricing
  rules; this decision does not define their taxable base.

## Alternatives rejected

- A Restaurant-wide rate cannot represent product or local Branch differences.
- A rate directly on MenuItem cannot differ by Branch.
- Client-submitted tax would violate authoritative billing.
- Recalculating old orders from current Catalog state would corrupt history.
