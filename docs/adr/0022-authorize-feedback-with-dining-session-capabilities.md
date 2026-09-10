# ADR 0022: Authorize feedback with DiningSession capabilities

- Status: Accepted
- Date: 2026-09-10

## Context

Feedback is anonymous, but an unauthenticated caller must not rate arbitrary
restaurants, branches, orders, or lines. V1 stored a three-state Happiness value
and free text against an order line without a migration policy for converting
that meaning into V2's five-point scale.

## Decision

Feedback is a separate four-layer module. Ordering remains authoritative for
eligibility and exposes a narrow snapshot only after the existing hash-only
DiningSession capability resolves to the exact completed order. A submission is
accepted for seven days after completion and may target either the order once or
each owned order line once. Partial PostgreSQL unique indexes enforce both rules.

Feedback stores tenant, order, line, and DiningSession identifiers, but never the
raw capability or an identity-provider subject. Rating is 1–5; sentiment is a
derived reporting value (1–2 Negative, 3 Neutral, 4–5 Positive). Staff moderation
hides/restores content without changing aggregates; hidden entries are excluded
from summaries. Comments are optional personal data and are not emitted in
integration events. A future product retention policy may anonymize or delete
comments independently of rating aggregates.

V1 Happiness values will not be silently converted. Migration must retain the
raw legacy value and wait for an approved, versioned mapping policy; unresolved
rows are reported for reconciliation.

## Consequences

- Possession of a live capability alone is insufficient; order ownership,
  completion, line membership, and time window are checked together.
- Management reads and moderation use permission plus Restaurant membership and
  always scope persistence queries by Restaurant.
- Repeated order-level and line-level submissions are race-safe.
- `FeedbackSubmittedV1` remains deferred until a concrete consumer exists; no
  comment text will be included when that event is introduced.
