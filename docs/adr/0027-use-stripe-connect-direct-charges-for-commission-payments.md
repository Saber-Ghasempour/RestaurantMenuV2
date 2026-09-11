# ADR 0027: Use Stripe Connect direct charges for commission payments

- Status: Accepted
- Date: 2026-09-11

## Context

RestaurantMenu charges no setup or recurring fee. Its revenue is an agreed 3%
commission on each paid bill, including discounts, tax, tips, and restaurant
fees in the final gross amount. Restaurants may operate in any country and
currency supported by Stripe Connect. Card data must never enter the API, and
Apple Pay and Google Pay should use the same payment flow as cards.

The platform is established in Portugal. A payments design must clearly assign
processor fees, disputes, refunds, settlement currency, webhook authority, and
accounting records instead of treating the provider response as the ledger.

## Decision

Use Stripe Connect Express connected accounts and create direct PaymentIntents
on the restaurant's connected account. Each PaymentIntent includes a 3%
`application_fee_amount`, calculated in integer minor units from:

`subtotal - discount + tax + tip + other fees`

The restaurant chooses its country and settlement currency during onboarding.
Stripe remains authoritative about supported account countries, currencies,
payment methods, verification, and payout eligibility. Payments remain disabled
until charges, payouts, and account details are ready. In this direct-charge
model, the connected restaurant account is responsible for Stripe processing
fees, refunds, disputes, and negative balances under its Stripe agreement; the
platform receives only its application fee.

The API returns a PaymentIntent client secret to a Stripe client integration.
Automatic payment methods permit eligible wallets, including Apple Pay and
Google Pay, after Stripe/domain/device prerequisites are satisfied. No card or
wallet credential is accepted or persisted by RestaurantMenu.

Payments owns an append-only event history plus a current aggregate snapshot in
the `payments` schema. The agreed commission rate, complete bill components,
connected account, currency, platform fee, and restaurant proceeds are
snapshotted per payment. One order can have one PaymentIntent. Provider event
IDs are unique, signatures and timestamp tolerance are verified before state
changes, and duplicate delivery is an idempotent no-op.

Authorized restaurant refunds are created through the platform with
`refund_application_fee=true`; the signed successful refund webhook then
reduces the recorded platform commission proportionally. A full refund therefore
reverses the full 3% fee; partial refunds reverse the rounded 3% attributable to
the cumulative refunded amount. Changing the commercial
rate later requires a new agreement workflow; it never rewrites past payments.

## Consequences

- There is no monthly or one-time platform billing model.
- Stripe-supported geography and cross-border rules bound the launch surface;
  "worldwide" does not mean every legal entity or payment method is eligible.
- Currency is immutable once the connected profile has accepted payments; a
  different settlement arrangement requires a controlled replacement flow.
- Wallet availability depends on Stripe Dashboard configuration, HTTPS, Apple
  domain registration where required, customer device, and country/currency.
- RestaurantMenu must reconcile Stripe balance/application-fee reports with its
  immutable payment events and retain webhook replay/incident procedures.
- The 3% basis and tax treatment require jurisdiction-specific legal and tax
  review; this ADR records product behavior, not tax advice.

## Alternatives rejected

- Stripe destination charges were rejected because the platform would normally
  pay processing fees and carry more refund/dispute liability.
- Platform subscriptions were rejected because the approved model has no
  recurring or setup charge.
- Collecting card data in the API was rejected because it expands PCI scope and
  duplicates provider security controls.
- Calculating commission from menu subtotal alone was rejected because the
  approved agreement applies to the complete final bill.
